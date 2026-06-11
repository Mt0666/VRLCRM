using Microsoft.EntityFrameworkCore;
using VRLCRM.Application.Payments;
using VRLCRM.Domain.Entities;
using VRLCRM.Domain.Enums;
using VRLCRM.Infrastructure.Data;

namespace VRLCRM.Infrastructure.Payments;

public class PaymentService : IPaymentService
{
    private readonly ApplicationDbContext _context;

    public PaymentService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Payment>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Payments
            .AsNoTracking()
            .Include(p => p.Customer)
            .Include(p => p.Supplier)
            .OrderByDescending(p => p.IsActive)
            .ThenByDescending(p => p.UpdatedAt ?? p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Payment?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Payments
            .Include(p => p.Customer)
            .Include(p => p.Supplier)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Payment> CreateIncomingPaymentAsync(int customerId, decimal amount, PaymentMethod method, DateTime paymentDate, string? notes, CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
        {
            throw new InvalidOperationException("Tahsilat tutarı 0'dan büyük olmalıdır.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == customerId && c.IsActive, cancellationToken) 
            ?? throw new InvalidOperationException("Müşteri bulunamadı.");

        var count = await _context.Payments.CountAsync(cancellationToken) + 1;
        var paymentNumber = $"TAH-{DateTime.UtcNow:yyyyMMdd}-{count:D4}";

        var payment = new Payment
        {
            PaymentNumber = paymentNumber,
            Type = PaymentType.Incoming,
            Method = method,
            Amount = amount,
            PaymentDate = paymentDate,
            CustomerId = customerId,
            Notes = notes?.Trim()
        };

        _context.Payments.Add(payment);

        // Tahsilat yapıldığında müşterinin borcu (bakiyesi) düşer
        customer.Balance -= amount;

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return payment;
    }

    public async Task<Payment> CreateOutgoingPaymentAsync(int supplierId, decimal amount, PaymentMethod method, DateTime paymentDate, string? notes, CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
        {
            throw new InvalidOperationException("Ödeme tutarı 0'dan büyük olmalıdır.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.Id == supplierId && s.IsActive, cancellationToken) 
            ?? throw new InvalidOperationException("Tedarikçi bulunamadı.");

        var count = await _context.Payments.CountAsync(cancellationToken) + 1;
        var paymentNumber = $"ODE-{DateTime.UtcNow:yyyyMMdd}-{count:D4}";

        var payment = new Payment
        {
            PaymentNumber = paymentNumber,
            Type = PaymentType.Outgoing,
            Method = method,
            Amount = amount,
            PaymentDate = paymentDate,
            SupplierId = supplierId,
            Notes = notes?.Trim()
        };

        _context.Payments.Add(payment);

        // Ödeme yapıldığında tedarikçiye olan borç (bakiyesi) düşer
        supplier.Balance -= amount;

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return payment;
    }

    public async Task<bool> DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        
        var payment = await _context.Payments
            .Include(p => p.Customer)
            .Include(p => p.Supplier)
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive, cancellationToken);
            
        if (payment is null)
        {
            return false;
        }

        payment.IsActive = false;

        // Fiş iptal edilirse, bakiye eski haline getirilir.
        if (payment.Type == PaymentType.Incoming && payment.Customer is not null)
        {
            payment.Customer.Balance += payment.Amount;
        }
        else if (payment.Type == PaymentType.Outgoing && payment.Supplier is not null)
        {
            payment.Supplier.Balance += payment.Amount;
        }

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        
        return true;
    }
}
