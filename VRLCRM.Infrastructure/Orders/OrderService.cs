using Microsoft.EntityFrameworkCore;
using VRLCRM.Application.Orders;
using VRLCRM.Domain.Entities;
using VRLCRM.Domain.Enums;
using VRLCRM.Infrastructure.Data;

namespace VRLCRM.Infrastructure.Orders;

public class OrderService : IOrderService
{
    private readonly ApplicationDbContext _context;

    public OrderService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.Lines)
            .ThenInclude(l => l.StockItem)
            .OrderByDescending(o => o.IsActive)
            .ThenByDescending(o => o.UpdatedAt ?? o.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Order?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Include(o => o.Customer!)
                .ThenInclude(c => c.Address)
            .Include(o => o.Lines)
            .ThenInclude(l => l.StockItem)
            .Include(o => o.SalesInvoice)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task<StockItem?> FindByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            return null;
        }

        var code = barcode.Trim();
        return await _context.StockItems
            .Include(s => s.Category)
            .FirstOrDefaultAsync(s => s.IsActive &&
                (s.Barcode == code || s.StockCode == code), cancellationToken);
    }

    public async Task<Order> CreateAndApproveAsync(
        int customerId,
        string? notes,
        decimal discountRate,
        IReadOnlyList<OrderLineInput> lines,
        CancellationToken cancellationToken = default)
    {
        if (lines.Count == 0)
        {
            throw new InvalidOperationException("Siparişte en az bir ürün olmalıdır.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == customerId && c.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Müşteri bulunamadı.");

        var orderLines = await BuildOrderLinesAsync(lines, cancellationToken);
        var normalizedRate = NormalizeDiscountRate(discountRate);
        var (subTotal, vatTotal, totalAmount) = CalculateTotals(orderLines, normalizedRate);

        if (!customer.IsUnlimitedCredit && !customer.HasSufficientCredit(totalAmount))
        {
            throw new InvalidOperationException(
                $"Cari limitiniz yetersiz. Mevcut borç: {customer.Balance:N2} ₺, Sipariş tutarı: {totalAmount:N2} ₺, Limit: {customer.EffectiveCreditLimit:N2} ₺");
        }

        var order = new Order
        {
            CustomerId = customer.Id,
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Approved,
            SubTotal = subTotal,
            VatTotal = vatTotal,
            DiscountRate = normalizedRate,
            TotalAmount = totalAmount,
            Notes = notes?.Trim(),
            Lines = orderLines,
            OrderNumber = await GenerateOrderNumberAsync(cancellationToken)
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return order;
    }

    public async Task<bool> UpdateAsync(
        int id,
        decimal discountRate,
        IReadOnlyList<OrderLineInput> lines,
        CancellationToken cancellationToken = default)
    {
        if (lines.Count == 0)
        {
            throw new InvalidOperationException("Siparişte en az bir ürün olmalıdır.");
        }

        var order = await _context.Orders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == id && o.IsActive, cancellationToken);

        if (order is null)
        {
            return false;
        }

        if (order.SalesInvoiceId.HasValue || order.Status == OrderStatus.Cancelled)
        {
            throw new InvalidOperationException("Faturalandırılmış veya iptal edilmiş sipariş düzenlenemez.");
        }

        _context.OrderLines.RemoveRange(order.Lines);

        var orderLines = await BuildOrderLinesAsync(lines, cancellationToken);
        var normalizedRate = NormalizeDiscountRate(discountRate);
        var (subTotal, vatTotal, totalAmount) = CalculateTotals(orderLines, normalizedRate);

        order.SubTotal = subTotal;
        order.VatTotal = vatTotal;
        order.DiscountRate = normalizedRate;
        order.TotalAmount = totalAmount;
        order.Lines = orderLines;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> CancelAsync(int id, CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == id && o.IsActive, cancellationToken);

        if (order is null || order.Status == OrderStatus.Cancelled)
        {
            return false;
        }

        if (order.SalesInvoiceId.HasValue)
        {
            throw new InvalidOperationException("Faturalandırılmış sipariş iptal edilemez.");
        }

        order.IsActive = false;
        order.Status = OrderStatus.Cancelled;
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    private async Task<List<OrderLine>> BuildOrderLinesAsync(
        IReadOnlyList<OrderLineInput> lines,
        CancellationToken cancellationToken)
    {
        var stockIds = lines.Select(l => l.StockItemId).Distinct().ToList();
        var stocks = await _context.StockItems
            .Where(s => stockIds.Contains(s.Id) && s.IsActive)
            .ToDictionaryAsync(s => s.Id, cancellationToken);

        if (stocks.Count != stockIds.Count)
        {
            throw new InvalidOperationException("Geçersiz veya pasif ürün seçildi.");
        }

        var orderLines = new List<OrderLine>();

        foreach (var line in lines)
        {
            if (line.Quantity <= 0)
            {
                throw new InvalidOperationException("Miktar 0'dan büyük olmalıdır.");
            }

            var stock = stocks[line.StockItemId];
            var vatRate = stock.VatRate;
            var lineSubTotal = line.Quantity * line.UnitPrice;
            var lineVatAmount = lineSubTotal * (vatRate / 100m);
            var lineTotal = lineSubTotal + lineVatAmount;

            orderLines.Add(new OrderLine
            {
                StockItemId = stock.Id,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                VatRate = vatRate,
                VatAmount = lineVatAmount,
                LineTotal = lineTotal,
                Notes = string.IsNullOrWhiteSpace(line.Notes) ? null : line.Notes.Trim()
            });
        }

        return orderLines;
    }

    private static decimal NormalizeDiscountRate(decimal discountRate) =>
        Math.Max(0, Math.Min(discountRate, 100));

    private static (decimal SubTotal, decimal VatTotal, decimal TotalAmount) CalculateTotals(
        IReadOnlyList<OrderLine> orderLines,
        decimal discountRate)
    {
        var subTotal = orderLines.Sum(l => l.Quantity * l.UnitPrice);
        var vatTotal = orderLines.Sum(l => l.VatAmount);
        var grossTotal = subTotal + vatTotal;
        var discountAmount = grossTotal * (discountRate / 100m);
        var totalAmount = grossTotal - discountAmount;
        return (subTotal, vatTotal, totalAmount);
    }

    private async Task<string> GenerateOrderNumberAsync(CancellationToken cancellationToken)
    {
        var count = await _context.Orders.CountAsync(cancellationToken) + 1;
        return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{count:D4}";
    }
}
