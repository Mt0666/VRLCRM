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
            .ThenByDescending(o => o.OrderDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<Order?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Include(o => o.Customer)
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

        var stockIds = lines.Select(l => l.StockItemId).Distinct().ToList();
        var stocks = await _context.StockItems
            .Where(s => stockIds.Contains(s.Id) && s.IsActive)
            .ToDictionaryAsync(s => s.Id, cancellationToken);

        if (stocks.Count != stockIds.Count)
        {
            throw new InvalidOperationException("Geçersiz veya pasif ürün seçildi.");
        }

        var orderLines = new List<OrderLine>();
        decimal subTotal = 0;
        decimal vatTotal = 0;

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

            subTotal += lineSubTotal;
            vatTotal += lineVatAmount;

            orderLines.Add(new OrderLine
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

        if (!customer.CanPlaceOrder(totalAmount))
        {
            throw new InvalidOperationException($"Kredi limiti aşıldı! Mevcut bakiye: {customer.Balance:N2} ₺, Sipariş Tutarı: {totalAmount:N2} ₺, Limit: {customer.CreditLimit:N2} ₺");
        }

        var order = new Order
        {
            CustomerId = customer.Id,
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Approved,
            SubTotal = subTotal,
            VatTotal = vatTotal,
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

    private async Task<string> GenerateOrderNumberAsync(CancellationToken cancellationToken)
    {
        var count = await _context.Orders.CountAsync(cancellationToken) + 1;
        return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{count:D4}";
    }
}
