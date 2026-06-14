using Microsoft.EntityFrameworkCore;
using VRLCRM.Application.Invoices;
using VRLCRM.Domain.Entities;
using VRLCRM.Domain.Enums;
using VRLCRM.Infrastructure.Data;

namespace VRLCRM.Infrastructure.Invoices;

public class InvoiceService : IInvoiceService
{
    private readonly ApplicationDbContext _context;

    public InvoiceService(ApplicationDbContext context)
    {
        _context = context;
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
        CancellationToken cancellationToken = default)
    {
        if (lines.Count == 0)
        {
            throw new InvalidOperationException("Faturada en az bir kalem olmalıdır.");
        }

        if (invoiceType == InvoiceType.Sales && !customerId.HasValue)
        {
            throw new InvalidOperationException("Satış faturası için müşteri seçilmelidir.");
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
                LineTotal = lineTotal
            });
        }

        var totalAmount = subTotal + vatTotal;

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
            TotalAmount = totalAmount,
            Lines = invoiceLines,
            InvoiceNumber = await GenerateInvoiceNumberAsync(invoiceType, cancellationToken)
        };

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync(cancellationToken);

        foreach (var line in invoiceLines)
        {
            var stock = await _context.StockItems
                .FirstAsync(s => s.Id == line.StockItemId, cancellationToken);

            if (invoiceType == InvoiceType.Sales)
            {
                stock.StockQuantity -= line.Quantity;
            }
            else
            {
                stock.StockQuantity += line.Quantity;
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

        if (invoiceType == InvoiceType.Sales && customer is not null)
        {
            customer.Balance += totalAmount;
        }

        if (invoiceType == InvoiceType.Purchase && supplier is not null)
        {
            supplier.Balance += totalAmount;
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
            .Include(o => o.Lines)
            .ThenInclude(l => l.StockItem)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Sipariş bulunamadı.");

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
                LineTotal = line.LineTotal
            });
        }

        if (!order.Customer.HasSufficientCredit(order.TotalAmount))
        {
            throw new InvalidOperationException($"Kredi limiti aşıldı! Mevcut bakiye: {order.Customer.Balance:N2} ₺, Fatura Tutarı: {order.TotalAmount:N2} ₺, Limit: {order.Customer.CreditLimit:N2} ₺");
        }

        var invoice = new Invoice
        {
            InvoiceType = InvoiceType.Sales,
            InvoiceDate = DateTime.UtcNow,
            CustomerId = order.CustomerId,
            Notes = $"Sipariş: {order.OrderNumber}" + (string.IsNullOrWhiteSpace(order.Notes) ? "" : $" | {order.Notes}"),
            SubTotal = order.SubTotal,
            VatTotal = order.VatTotal,
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

        order.Customer.Balance += invoice.TotalAmount;
        order.SalesInvoiceId = invoice.Id;
        
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return invoice;
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

        // Bakiyeleri geri al
        if (invoice.InvoiceType == InvoiceType.Sales && invoice.CustomerId.HasValue)
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == invoice.CustomerId, cancellationToken);
            if (customer is not null)
                customer.Balance -= invoice.TotalAmount;
        }
        else if (invoice.InvoiceType == InvoiceType.Purchase && invoice.SupplierId.HasValue)
        {
            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(s => s.Id == invoice.SupplierId, cancellationToken);
            if (supplier is not null)
                supplier.Balance -= invoice.TotalAmount;
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

            var categoryExists = await _context.Categories
                .AnyAsync(c => c.Id == input.CategoryId && c.IsActive, cancellationToken);
            if (!categoryExists)
            {
                throw new InvalidOperationException("Geçerli bir kategori seçin.");
            }

            var stock = new StockItem
            {
                StockCode = stockCode,
                Name = name,
                CategoryId = input.CategoryId,
                Barcode = input.Barcode?.Trim(),
                Price = line.UnitPrice,
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

    private async Task<string> GenerateInvoiceNumberAsync(InvoiceType type, CancellationToken cancellationToken)
    {
        var prefix = type == InvoiceType.Sales ? "SF" : "AF";
        var count = await _context.Invoices.CountAsync(cancellationToken) + 1;
        return $"{prefix}-{DateTime.UtcNow:yyyyMMdd}-{count:D4}";
    }
}
