using Microsoft.EntityFrameworkCore;
using VRLCRM.Application.Balances;
using VRLCRM.Application.Invoices;
using VRLCRM.Domain.Entities;
using VRLCRM.Domain.Enums;
using VRLCRM.Infrastructure.Data;

namespace VRLCRM.Infrastructure.Invoices;

public class InvoiceService : IInvoiceService
{
    private readonly ApplicationDbContext _context;
    private readonly IBalanceRecalculationService _balanceRecalculation;

    public InvoiceService(
        ApplicationDbContext context,
        IBalanceRecalculationService balanceRecalculation)
    {
        _context = context;
        _balanceRecalculation = balanceRecalculation;
    }

    public async Task<IReadOnlyList<Invoice>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Invoices
            .AsNoTracking()
            .Include(i => i.Customer)
            .Include(i => i.Supplier)
            .Include(i => i.Lines)
            .ThenInclude(l => l.StockItem)
            .OrderByDescending(i => i.IsActive)
            .ThenByDescending(i => i.UpdatedAt ?? i.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Invoice>> GetByTypeAsync(
        InvoiceType invoiceType,
        CancellationToken cancellationToken = default)
    {
        return await _context.Invoices
            .AsNoTracking()
            .Include(i => i.Customer)
            .Include(i => i.Supplier)
            .Include(i => i.Lines)
            .ThenInclude(l => l.StockItem)
            .Where(i => i.InvoiceType == invoiceType)
            .OrderByDescending(i => i.IsActive)
            .ThenByDescending(i => i.UpdatedAt ?? i.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Invoice?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Invoices
            .Include(i => i.Customer!)
                .ThenInclude(c => c.Address)
            .Include(i => i.Supplier)
            .Include(i => i.Lines)
            .ThenInclude(l => l.StockItem)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<Invoice> CreateAsync(
        InvoiceType invoiceType,
        int? customerId,
        int? supplierId,
        DateTime invoiceDate,
        string? notes,
        IReadOnlyList<InvoiceLineInput> lines,
        decimal discountRate = 0m,
        CancellationToken cancellationToken = default)
    {
        if (lines.Count == 0)
        {
            throw new InvalidOperationException("Faturada en az bir kalem olmalıdır.");
        }

        if (invoiceType == InvoiceType.Sales && !customerId.HasValue && !supplierId.HasValue)
        {
            throw new InvalidOperationException("Satış faturası için müşteri veya tedarikçi seçilmelidir.");
        }

        if (invoiceType == InvoiceType.Sales && customerId.HasValue && supplierId.HasValue)
        {
            throw new InvalidOperationException("Satış faturasında yalnızca müşteri veya tedarikçi seçilebilir.");
        }

        if (invoiceType == InvoiceType.Purchase && !supplierId.HasValue)
        {
            throw new InvalidOperationException("Alış faturası için tedarikçi seçilmelidir.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        Customer? customer = null;
        if (customerId.HasValue)
        {
            customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == customerId && c.IsActive, cancellationToken)
                ?? throw new InvalidOperationException("Müşteri bulunamadı.");
        }

        Supplier? supplier = null;
        if (supplierId.HasValue)
        {
            supplier = await _context.Suppliers
                .FirstOrDefaultAsync(s => s.Id == supplierId && s.IsActive, cancellationToken)
                ?? throw new InvalidOperationException("Tedarikçi bulunamadı.");
        }

        var invoiceLines = new List<InvoiceLine>();
        var invoiceLineSalePrices = new List<decimal?>(); // InvoiceLine ile paralel — kullanıcının girdiği satış fiyatı
        decimal subTotal = 0;
        decimal vatTotal = 0;

        foreach (var line in lines)
        {
            if (line.Quantity <= 0)
            {
                throw new InvalidOperationException("Miktar 0'dan büyük olmalıdır.");
            }

            var stock = await ResolveStockForLineAsync(invoiceType, line, cancellationToken);
            
            var vatRate = stock.VatRate;
            var lineSubTotal = line.Quantity * line.UnitPrice;
            var lineVatAmount = lineSubTotal * (vatRate / 100m);
            var lineTotal = lineSubTotal + lineVatAmount;

            subTotal += lineSubTotal;
            vatTotal += lineVatAmount;

            if (invoiceType == InvoiceType.Sales && stock.StockQuantity < line.Quantity)
            {
                throw new InvalidOperationException($"{stock.Name} için yeterli stok yok.");
            }

            invoiceLines.Add(new InvoiceLine
            {
                StockItemId = stock.Id,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                VatRate = vatRate,
                VatAmount = lineVatAmount,
                LineTotal = lineTotal,
                Notes = string.IsNullOrWhiteSpace(line.Notes) ? null : line.Notes.Trim()
            });
            invoiceLineSalePrices.Add(line.SalePrice);
        }

        var grossTotal = subTotal + vatTotal;
        var clampedRate = Math.Min(Math.Max(discountRate, 0m), 100m);
        var discountAmount = Math.Round(grossTotal * clampedRate / 100m, 2);
        var totalAmount = grossTotal - discountAmount;

        if (invoiceType == InvoiceType.Sales && customer is not null && !customer.HasSufficientCredit(totalAmount))
        {
            throw new InvalidOperationException($"Kredi limiti aşıldı! Mevcut bakiye: {customer.Balance:N2} ₺, Fatura Tutarı: {totalAmount:N2} ₺, Limit: {customer.CreditLimit:N2} ₺");
        }

        var invoice = new Invoice
        {
            InvoiceType = invoiceType,
            InvoiceDate = invoiceDate,
            CustomerId = customerId,
            SupplierId = supplierId,
            Notes = notes?.Trim(),
            SubTotal = subTotal,
            VatTotal = vatTotal,
            DiscountRate = clampedRate,
            DiscountAmount = discountAmount,
            TotalAmount = totalAmount,
            Lines = invoiceLines,
            InvoiceNumber = await GenerateInvoiceNumberAsync(invoiceType, cancellationToken)
        };

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync(cancellationToken);

        for (int i = 0; i < invoiceLines.Count; i++)
        {
            var line = invoiceLines[i];
            var salePrice = invoiceLineSalePrices[i];

            var stock = await _context.StockItems
                .FirstAsync(s => s.Id == line.StockItemId, cancellationToken);

            if (invoiceType == InvoiceType.Sales)
            {
                stock.StockQuantity -= line.Quantity;
            }
            else
            {
                stock.StockQuantity += line.Quantity;
                // Alış fiyatını güncelle; satış fiyatını kullanıcı girdiyse onu, yoksa alış × 1.30 kullan
                stock.PurchasePrice = line.UnitPrice;
                stock.Price = salePrice.HasValue && salePrice.Value > 0
                    ? salePrice.Value
                    : Math.Round(line.UnitPrice * 1.30m, 2);
            }

            _context.StockMovements.Add(new StockMovement
            {
                StockItemId = stock.Id,
                MovementType = invoiceType == InvoiceType.Sales ? StockMovementType.Out : StockMovementType.In,
                Quantity = line.Quantity,
                ReferenceType = StockMovementReferenceType.Invoice,
                ReferenceId = invoice.Id,
                MovementDate = invoiceDate
            });
        }

        if (customerId.HasValue)
        {
            await _balanceRecalculation.RecalculateCustomerBalanceAsync(customerId.Value, cancellationToken);
        }

        if (supplierId.HasValue)
        {
            await _balanceRecalculation.RecalculateSupplierBalanceAsync(supplierId.Value, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return invoice;
    }

    public async Task<Invoice> CreateSalesInvoiceFromOrderAsync(
        int orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Supplier)
            .Include(o => o.Lines)
            .ThenInclude(l => l.StockItem)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Sipariş bulunamadı.");

        if (!order.CustomerId.HasValue && !order.SupplierId.HasValue)
        {
            throw new InvalidOperationException("Siparişin bağlı olduğu cari bulunamadı.");
        }

        if (order.Status != OrderStatus.Approved)
        {
            throw new InvalidOperationException("Yalnızca onaylı siparişler faturalandırılabilir.");
        }

        if (order.SalesInvoiceId.HasValue)
        {
            throw new InvalidOperationException("Bu sipariş zaten satış faturasına dönüştürülmüş.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var invoiceLines = new List<InvoiceLine>();
        foreach (var line in order.Lines)
        {
            if (line.StockItem.StockQuantity < line.Quantity)
            {
                throw new InvalidOperationException($"{line.StockItem.Name} için yeterli stok yok.");
            }

            invoiceLines.Add(new InvoiceLine
            {
                StockItemId = line.StockItemId,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                VatRate = line.VatRate,
                VatAmount = line.VatAmount,
                LineTotal = line.LineTotal,
                Notes = string.IsNullOrWhiteSpace(line.Notes) ? null : line.Notes.Trim()
            });
        }

        if (order.CustomerId.HasValue)
        {
            if (!order.Customer!.HasSufficientCredit(order.TotalAmount))
            {
                throw new InvalidOperationException($"Kredi limiti aşıldı! Mevcut bakiye: {order.Customer.Balance:N2} ₺, Fatura Tutarı: {order.TotalAmount:N2} ₺, Limit: {order.Customer.CreditLimit:N2} ₺");
            }
        }

        var invoice = new Invoice
        {
            InvoiceType = InvoiceType.Sales,
            InvoiceDate = DateTime.UtcNow,
            CustomerId = order.CustomerId,
            SupplierId = order.SupplierId,
            Notes = $"Sipariş: {order.OrderNumber}" + (string.IsNullOrWhiteSpace(order.Notes) ? "" : $" | {order.Notes}"),
            SubTotal = order.SubTotal,
            VatTotal = order.VatTotal,
            DiscountRate = order.DiscountRate,
            DiscountAmount = Math.Round(order.GrossTotal * order.DiscountRate / 100m, 2),
            TotalAmount = order.TotalAmount,
            Lines = invoiceLines,
            InvoiceNumber = await GenerateInvoiceNumberAsync(InvoiceType.Sales, cancellationToken)
        };

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync(cancellationToken);

        foreach (var line in invoiceLines)
        {
            var stock = await _context.StockItems.FirstAsync(s => s.Id == line.StockItemId, cancellationToken);
            stock.StockQuantity -= line.Quantity;

            _context.StockMovements.Add(new StockMovement
            {
                StockItemId = stock.Id,
                MovementType = StockMovementType.Out,
                Quantity = line.Quantity,
                ReferenceType = StockMovementReferenceType.Invoice,
                ReferenceId = invoice.Id,
                MovementDate = invoice.InvoiceDate,
                Notes = $"Siparişten Dönüştürülen Fatura: {invoice.InvoiceNumber}"
            });
        }

        order.SalesInvoiceId = invoice.Id;

        if (order.CustomerId.HasValue)
        {
            await _balanceRecalculation.RecalculateCustomerBalanceAsync(order.CustomerId.Value, cancellationToken);
        }

        if (order.SupplierId.HasValue)
        {
            await _balanceRecalculation.RecalculateSupplierBalanceAsync(order.SupplierId.Value, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return invoice;
    }

    public async Task<bool> UpdateSalesInvoiceAsync(
        int id,
        decimal discountRate,
        IReadOnlyList<InvoiceLineUpdateInput> lines,
        CancellationToken cancellationToken = default)
    {
        if (lines.Count == 0)
        {
            throw new InvalidOperationException("Faturada en az bir kalem olmalıdır.");
        }

        var invoice = await _context.Invoices
            .Include(i => i.Lines)
            .ThenInclude(l => l.StockItem)
            .FirstOrDefaultAsync(i => i.Id == id && i.IsActive, cancellationToken);

        if (invoice is null)
        {
            return false;
        }

        if (invoice.InvoiceType != InvoiceType.Sales)
        {
            throw new InvalidOperationException("Yalnızca satış faturaları düzenlenebilir.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        Customer? customer = null;
        Supplier? supplier = null;

        if (invoice.CustomerId.HasValue)
        {
            customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == invoice.CustomerId && c.IsActive, cancellationToken)
                ?? throw new InvalidOperationException("Müşteri bulunamadı.");
        }
        else if (invoice.SupplierId.HasValue)
        {
            supplier = await _context.Suppliers
                .FirstOrDefaultAsync(s => s.Id == invoice.SupplierId && s.IsActive, cancellationToken)
                ?? throw new InvalidOperationException("Tedarikçi bulunamadı.");
        }
        else
        {
            throw new InvalidOperationException("Satış faturasının cari bilgisi bulunamadı.");
        }

        var oldTotal = invoice.TotalAmount;
        var now = DateTime.UtcNow;
        var oldQtyByStock = AggregateQuantities(invoice.Lines.Select(l => (l.StockItemId, l.Quantity)));

        var allStockIds = oldQtyByStock.Keys
            .Union(lines.Select(l => l.StockItemId))
            .Distinct()
            .ToList();

        var stocks = await _context.StockItems
            .Where(s => allStockIds.Contains(s.Id) && s.IsActive)
            .ToDictionaryAsync(s => s.Id, cancellationToken);

        if (stocks.Count != allStockIds.Count)
        {
            throw new InvalidOperationException("Geçersiz veya pasif ürün seçildi.");
        }

        var newLines = BuildInvoiceLinesFromUpdateInput(lines, stocks, out var subTotal, out var vatTotal);
        var newQtyByStock = AggregateQuantities(lines.Select(l => (l.StockItemId, l.Quantity)));

        var grossTotal = subTotal + vatTotal;
        var clampedRate = Math.Min(Math.Max(discountRate, 0m), 100m);
        var discountAmount = Math.Round(grossTotal * clampedRate / 100m, 2);
        var newTotal = grossTotal - discountAmount;

        var balanceDelta = newTotal - oldTotal;
        if (customer is not null && balanceDelta > 0 && !customer.HasSufficientCredit(balanceDelta))
        {
            throw new InvalidOperationException(
                $"Kredi limiti aşıldı! Mevcut bakiye: {customer.Balance:N2} ₺, Artış: {balanceDelta:N2} ₺, Limit: {customer.EffectiveCreditLimit:N2} ₺");
        }

        ApplyNetInvoiceEditStockMovements(
            InvoiceType.Sales,
            invoice.Id,
            invoice.InvoiceNumber,
            now,
            oldQtyByStock,
            newQtyByStock,
            stocks);

        _context.InvoiceLines.RemoveRange(invoice.Lines);

        invoice.SubTotal = subTotal;
        invoice.VatTotal = vatTotal;
        invoice.DiscountRate = clampedRate;
        invoice.DiscountAmount = discountAmount;
        invoice.TotalAmount = newTotal;
        invoice.Lines = newLines;

        await _context.SaveChangesAsync(cancellationToken);

        if (customer is not null)
        {
            await _balanceRecalculation.RecalculateCustomerBalanceAsync(customer.Id, cancellationToken);
        }
        else if (supplier is not null)
        {
            await _balanceRecalculation.RecalculateSupplierBalanceAsync(supplier.Id, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> UpdatePurchaseInvoiceAsync(
        int id,
        decimal discountRate,
        IReadOnlyList<InvoiceLineUpdateInput> lines,
        CancellationToken cancellationToken = default)
    {
        if (lines.Count == 0)
        {
            throw new InvalidOperationException("Faturada en az bir kalem olmalıdır.");
        }

        var invoice = await _context.Invoices
            .Include(i => i.Lines)
            .ThenInclude(l => l.StockItem)
            .FirstOrDefaultAsync(i => i.Id == id && i.IsActive, cancellationToken);

        if (invoice is null)
        {
            return false;
        }

        if (invoice.InvoiceType != InvoiceType.Purchase)
        {
            throw new InvalidOperationException("Yalnızca alış faturaları düzenlenebilir.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var supplier = await _context.Suppliers
            .FirstOrDefaultAsync(s => s.Id == invoice.SupplierId && s.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Tedarikçi bulunamadı.");

        var oldTotal = invoice.TotalAmount;
        var now = DateTime.UtcNow;
        var oldQtyByStock = AggregateQuantities(invoice.Lines.Select(l => (l.StockItemId, l.Quantity)));

        var allStockIds = oldQtyByStock.Keys
            .Union(lines.Select(l => l.StockItemId))
            .Distinct()
            .ToList();

        var stocks = await _context.StockItems
            .Where(s => allStockIds.Contains(s.Id) && s.IsActive)
            .ToDictionaryAsync(s => s.Id, cancellationToken);

        if (stocks.Count != allStockIds.Count)
        {
            throw new InvalidOperationException("Geçersiz veya pasif ürün seçildi.");
        }

        var newLines = BuildInvoiceLinesFromUpdateInput(lines, stocks, out var subTotal, out var vatTotal);
        var newQtyByStock = AggregateQuantities(lines.Select(l => (l.StockItemId, l.Quantity)));
        var newUnitPricesByStock = lines
            .GroupBy(l => l.StockItemId)
            .ToDictionary(g => g.Key, g => g.Last().UnitPrice);

        var grossTotal = subTotal + vatTotal;
        var clampedRate = Math.Min(Math.Max(discountRate, 0m), 100m);
        var discountAmount = Math.Round(grossTotal * clampedRate / 100m, 2);
        var newTotal = grossTotal - discountAmount;

        ApplyNetInvoiceEditStockMovements(
            InvoiceType.Purchase,
            invoice.Id,
            invoice.InvoiceNumber,
            now,
            oldQtyByStock,
            newQtyByStock,
            stocks,
            newUnitPricesByStock);

        _context.InvoiceLines.RemoveRange(invoice.Lines);

        invoice.SubTotal = subTotal;
        invoice.VatTotal = vatTotal;
        invoice.DiscountRate = clampedRate;
        invoice.DiscountAmount = discountAmount;
        invoice.TotalAmount = newTotal;
        invoice.Lines = newLines;

        await _context.SaveChangesAsync(cancellationToken);
        await _balanceRecalculation.RecalculateSupplierBalanceAsync(supplier.Id, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var invoice = await _context.Invoices
            .Include(i => i.Lines)
            .ThenInclude(l => l.StockItem)
            .FirstOrDefaultAsync(i => i.Id == id && i.IsActive, cancellationToken);

        if (invoice is null)
            return false;

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        invoice.IsActive = false;

        // Stok hareketlerini tersine çevir
        foreach (var line in invoice.Lines)
        {
            if (invoice.InvoiceType == InvoiceType.Sales)
            {
                line.StockItem.StockQuantity += line.Quantity;
                _context.StockMovements.Add(new StockMovement
                {
                    StockItemId = line.StockItemId,
                    MovementType = StockMovementType.In,
                    Quantity = line.Quantity,
                    ReferenceType = StockMovementReferenceType.Invoice,
                    ReferenceId = invoice.Id,
                    MovementDate = DateTime.UtcNow,
                    Notes = $"İptal: {invoice.InvoiceNumber}"
                });
            }
            else
            {
                line.StockItem.StockQuantity -= line.Quantity;
                _context.StockMovements.Add(new StockMovement
                {
                    StockItemId = line.StockItemId,
                    MovementType = StockMovementType.Out,
                    Quantity = line.Quantity,
                    ReferenceType = StockMovementReferenceType.Invoice,
                    ReferenceId = invoice.Id,
                    MovementDate = DateTime.UtcNow,
                    Notes = $"İptal: {invoice.InvoiceNumber}"
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Bakiyeleri kaynak veriden yeniden hesapla
        if (invoice.CustomerId.HasValue)
        {
            await _balanceRecalculation.RecalculateCustomerBalanceAsync(invoice.CustomerId.Value, cancellationToken);
        }

        if (invoice.SupplierId.HasValue)
        {
            await _balanceRecalculation.RecalculateSupplierBalanceAsync(invoice.SupplierId.Value, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private async Task<StockItem> ResolveStockForLineAsync(
        InvoiceType invoiceType,
        InvoiceLineInput line,
        CancellationToken cancellationToken)
    {
        if (invoiceType == InvoiceType.Purchase && line.NewProduct is not null)
        {
            var input = line.NewProduct;
            var stockCode = input.StockCode.Trim();
            var name = input.Name.Trim();

            if (string.IsNullOrWhiteSpace(stockCode) || string.IsNullOrWhiteSpace(name))
            {
                throw new InvalidOperationException("Yeni ürün için stok kodu ve ürün adı zorunludur.");
            }

            if (await _context.StockItems.AnyAsync(s => s.StockCode == stockCode, cancellationToken))
            {
                throw new InvalidOperationException($"'{stockCode}' stok kodu zaten kullanılıyor.");
            }

            if (!string.IsNullOrWhiteSpace(input.Barcode) &&
                await _context.StockItems.AnyAsync(s => s.Barcode == input.Barcode.Trim(), cancellationToken))
            {
                throw new InvalidOperationException($"'{input.Barcode.Trim()}' barkodu zaten kullanılıyor.");
            }

            if (await _context.StockItems.AnyAsync(
                    s => s.Name.ToLower() == name.ToLower(),
                    cancellationToken))
            {
                throw new InvalidOperationException($"'{name}' adlı ürün zaten kayıtlı.");
            }

            var categoryId = await ResolveCategoryIdForNewProductAsync(input, cancellationToken);

            var stock = new StockItem
            {
                StockCode = stockCode,
                Name = name,
                CategoryId = categoryId,
                Barcode = input.Barcode?.Trim(),
                PurchasePrice = line.UnitPrice,
                Price = line.SalePrice.HasValue && line.SalePrice.Value > 0
                    ? line.SalePrice.Value
                    : Math.Round(line.UnitPrice * 1.30m, 2),
                VatRate = input.VatRate,
                StockQuantity = 0,
                CriticalStockLevel = input.CriticalStockLevel > 0 ? input.CriticalStockLevel : 5,
                IsActive = true
            };

            _context.StockItems.Add(stock);
            await _context.SaveChangesAsync(cancellationToken);
            return stock;
        }

        if (!line.StockItemId.HasValue)
        {
            throw new InvalidOperationException("Stok kalemi seçilmelidir.");
        }

        return await _context.StockItems
            .FirstOrDefaultAsync(s => s.Id == line.StockItemId && s.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Geçersiz veya pasif stok kalemi seçildi.");
    }

    private async Task<int> ResolveCategoryIdForNewProductAsync(
        NewPurchaseProductInput input,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(input.NewCategoryName))
        {
            var name = input.NewCategoryName.Trim();
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Name == name, cancellationToken);

            if (category is null)
            {
                category = new Category
                {
                    Name = name,
                    IsActive = true
                };
                _context.Categories.Add(category);
                await _context.SaveChangesAsync(cancellationToken);
                return category.Id;
            }

            if (!category.IsActive)
            {
                category.IsActive = true;
                await _context.SaveChangesAsync(cancellationToken);
            }

            return category.Id;
        }

        if (input.CategoryId > 0 &&
            await _context.Categories.AnyAsync(c => c.Id == input.CategoryId && c.IsActive, cancellationToken))
        {
            return input.CategoryId;
        }

        throw new InvalidOperationException("Geçerli bir kategori seçin veya yeni kategori adı girin.");
    }

    private static Dictionary<int, int> AggregateQuantities(IEnumerable<(int StockItemId, int Quantity)> items)
    {
        var result = new Dictionary<int, int>();
        foreach (var (stockItemId, quantity) in items)
        {
            result[stockItemId] = result.GetValueOrDefault(stockItemId) + quantity;
        }

        return result;
    }

    private static List<InvoiceLine> BuildInvoiceLinesFromUpdateInput(
        IReadOnlyList<InvoiceLineUpdateInput> lines,
        IReadOnlyDictionary<int, StockItem> stocks,
        out decimal subTotal,
        out decimal vatTotal)
    {
        subTotal = 0;
        vatTotal = 0;
        var invoiceLines = new List<InvoiceLine>();

        foreach (var line in lines)
        {
            if (line.Quantity <= 0)
            {
                throw new InvalidOperationException("Miktar 0'dan büyük olmalıdır.");
            }

            if (line.VatRate < 0 || line.VatRate > 100)
            {
                throw new InvalidOperationException("KDV oranı 0-100 arasında olmalıdır.");
            }

            if (!stocks.TryGetValue(line.StockItemId, out var stock))
            {
                throw new InvalidOperationException("Geçersiz veya pasif ürün seçildi.");
            }

            var lineSubTotal = line.Quantity * line.UnitPrice;
            var lineVatAmount = lineSubTotal * (line.VatRate / 100m);
            var lineTotal = lineSubTotal + lineVatAmount;

            subTotal += lineSubTotal;
            vatTotal += lineVatAmount;

            invoiceLines.Add(new InvoiceLine
            {
                StockItemId = stock.Id,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                VatRate = line.VatRate,
                VatAmount = lineVatAmount,
                LineTotal = lineTotal,
                Notes = string.IsNullOrWhiteSpace(line.Notes) ? null : line.Notes.Trim()
            });
        }

        return invoiceLines;
    }

    private void ApplyNetInvoiceEditStockMovements(
        InvoiceType invoiceType,
        int invoiceId,
        string invoiceNumber,
        DateTime movementDate,
        Dictionary<int, int> oldQtyByStock,
        Dictionary<int, int> newQtyByStock,
        Dictionary<int, StockItem> stocks,
        Dictionary<int, decimal>? newUnitPricesByStock = null)
    {
        foreach (var stockId in oldQtyByStock.Keys.Union(newQtyByStock.Keys))
        {
            var oldQty = oldQtyByStock.GetValueOrDefault(stockId);
            var newQty = newQtyByStock.GetValueOrDefault(stockId);
            var stockDelta = invoiceType == InvoiceType.Sales ? oldQty - newQty : newQty - oldQty;

            if (!stocks.TryGetValue(stockId, out var stock))
            {
                continue;
            }

            if (invoiceType == InvoiceType.Purchase &&
                newUnitPricesByStock?.TryGetValue(stockId, out var unitPrice) == true)
            {
                stock.PurchasePrice = unitPrice;
                stock.Price = Math.Round(unitPrice * 1.30m, 2);
            }

            if (stockDelta == 0)
            {
                continue;
            }

            if (stockDelta < 0 && stock.StockQuantity < Math.Abs(stockDelta))
            {
                throw new InvalidOperationException(
                    $"{stock.Name} için stok yetersiz. Fatura düzenlenemez; stoktan çıkış yapılmış olabilir.");
            }

            stock.StockQuantity += stockDelta;

            var qty = Math.Abs(stockDelta);
            var isIn = stockDelta > 0;
            var noteAction = invoiceType switch
            {
                InvoiceType.Sales when isIn => "iade",
                InvoiceType.Sales => "çıkış",
                InvoiceType.Purchase when isIn => "giriş",
                _ => "geri alındı"
            };

            _context.StockMovements.Add(new StockMovement
            {
                StockItemId = stockId,
                MovementType = isIn ? StockMovementType.In : StockMovementType.Out,
                Quantity = qty,
                ReferenceType = StockMovementReferenceType.Invoice,
                ReferenceId = invoiceId,
                MovementDate = movementDate,
                Notes = $"Fatura düzenleme - stok {(isIn ? "girişi" : "çıkışı")}: {invoiceNumber} ({qty} adet {noteAction})"
            });
        }
    }

    private async Task<string> GenerateInvoiceNumberAsync(InvoiceType type, CancellationToken cancellationToken)
    {
        var prefix = type == InvoiceType.Sales ? "SF" : "AF";
        var count = await _context.Invoices.CountAsync(cancellationToken) + 1;
        return $"{prefix}-{DateTime.UtcNow:yyyyMMdd}-{count:D4}";
    }
}
