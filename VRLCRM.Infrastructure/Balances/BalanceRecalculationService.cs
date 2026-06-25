using Microsoft.EntityFrameworkCore;
using VRLCRM.Application.Balances;
using VRLCRM.Domain.Enums;
using VRLCRM.Infrastructure.Data;

namespace VRLCRM.Infrastructure.Balances;

public class BalanceRecalculationService : IBalanceRecalculationService
{
    private readonly ApplicationDbContext _context;

    public BalanceRecalculationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task RecalculateCustomerBalanceAsync(int customerId, CancellationToken cancellationToken = default)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken);

        if (customer is null)
        {
            return;
        }

        var invoiceTotal = await _context.Invoices
            .Where(i => i.CustomerId == customerId && i.IsActive && i.InvoiceType == InvoiceType.Sales)
            .SumAsync(i => i.TotalAmount, cancellationToken);

        var payments = await _context.Payments
            .Where(p => p.CustomerId == customerId && p.IsActive)
            .ToListAsync(cancellationToken);

        var incomingTotal = payments
            .Where(p => p.Type == PaymentType.Incoming)
            .Sum(p => p.Amount);

        var outgoingTotal = payments
            .Where(p => p.Type == PaymentType.Outgoing)
            .Sum(p => p.Amount);

        customer.Balance = invoiceTotal - incomingTotal + outgoingTotal;
    }

    public async Task RecalculateSupplierBalanceAsync(int supplierId, CancellationToken cancellationToken = default)
    {
        var supplier = await _context.Suppliers
            .FirstOrDefaultAsync(s => s.Id == supplierId, cancellationToken);

        if (supplier is null)
        {
            return;
        }

        var invoiceTotal = await _context.Invoices
            .Where(i => i.SupplierId == supplierId && i.IsActive && i.InvoiceType == InvoiceType.Purchase)
            .SumAsync(i => i.TotalAmount, cancellationToken);

        var salesInvoiceTotal = await _context.Invoices
            .Where(i => i.SupplierId == supplierId && i.IsActive && i.InvoiceType == InvoiceType.Sales)
            .SumAsync(i => i.TotalAmount, cancellationToken);

        var payments = await _context.Payments
            .Where(p => p.SupplierId == supplierId && p.IsActive)
            .ToListAsync(cancellationToken);

        var outgoingTotal = payments
            .Where(p => p.Type == PaymentType.Outgoing)
            .Sum(p => p.Amount);

        var incomingTotal = payments
            .Where(p => p.Type == PaymentType.Incoming)
            .Sum(p => p.Amount);

        supplier.Balance = invoiceTotal - salesInvoiceTotal - outgoingTotal + incomingTotal;
    }

    public async Task RecalculateAllAsync(CancellationToken cancellationToken = default)
    {
        var customerIds = await _context.Customers
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        foreach (var customerId in customerIds)
        {
            await RecalculateCustomerBalanceAsync(customerId, cancellationToken);
        }

        var supplierIds = await _context.Suppliers
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        foreach (var supplierId in supplierIds)
        {
            await RecalculateSupplierBalanceAsync(supplierId, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
