using Microsoft.EntityFrameworkCore;
using VRLCRM.Application.Balances;
using VRLCRM.Application.Payments;
using VRLCRM.Domain.Entities;
using VRLCRM.Domain.Enums;
using VRLCRM.Infrastructure.Data;

namespace VRLCRM.Infrastructure.Payments;

public class PaymentService : IPaymentService
{
    private readonly ApplicationDbContext _context;
    private readonly IBalanceRecalculationService _balanceRecalculation;

    public PaymentService(
        ApplicationDbContext context,
        IBalanceRecalculationService balanceRecalculation)
    {
        _context = context;
        _balanceRecalculation = balanceRecalculation;
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

        _ = await _context.Customers.FirstOrDefaultAsync(c => c.Id == customerId && c.IsActive, cancellationToken)
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
        await _context.SaveChangesAsync(cancellationToken);
        await _balanceRecalculation.RecalculateCustomerBalanceAsync(customerId, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return payment;
    }

    public async Task<Payment> CreateOutgoingPaymentToCustomerAsync(
        int customerId,
        decimal amount,
        PaymentMethod method,
        DateTime paymentDate,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
        {
            throw new InvalidOperationException("Ödeme tutarı 0'dan büyük olmalıdır.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        _ = await _context.Customers.FirstOrDefaultAsync(c => c.Id == customerId && c.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Müşteri bulunamadı.");

        var payment = new Payment
        {
            PaymentNumber = await GeneratePaymentNumberAsync("MOD", cancellationToken),
            Type = PaymentType.Outgoing,
            Method = method,
            Amount = amount,
            PaymentDate = paymentDate,
            CustomerId = customerId,
            Notes = notes?.Trim()
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync(cancellationToken);
        await _balanceRecalculation.RecalculateCustomerBalanceAsync(customerId, cancellationToken);
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

        _ = await _context.Suppliers.FirstOrDefaultAsync(s => s.Id == supplierId && s.IsActive, cancellationToken)
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
        await _context.SaveChangesAsync(cancellationToken);
        await _balanceRecalculation.RecalculateSupplierBalanceAsync(supplierId, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return payment;
    }

    public async Task<Payment> CreateIncomingPaymentFromSupplierAsync(
        int supplierId,
        decimal amount,
        PaymentMethod method,
        DateTime paymentDate,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
        {
            throw new InvalidOperationException("Tahsilat tutarı 0'dan büyük olmalıdır.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        _ = await _context.Suppliers.FirstOrDefaultAsync(s => s.Id == supplierId && s.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Tedarikçi bulunamadı.");

        var payment = new Payment
        {
            PaymentNumber = await GeneratePaymentNumberAsync("TTG", cancellationToken),
            Type = PaymentType.Incoming,
            Method = method,
            Amount = amount,
            PaymentDate = paymentDate,
            SupplierId = supplierId,
            Notes = notes?.Trim()
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync(cancellationToken);
        await _balanceRecalculation.RecalculateSupplierBalanceAsync(supplierId, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return payment;
    }

    public async Task<bool> DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive, cancellationToken);

        if (payment is null)
        {
            return false;
        }

        payment.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);

        if (payment.CustomerId.HasValue)
        {
            await _balanceRecalculation.RecalculateCustomerBalanceAsync(payment.CustomerId.Value, cancellationToken);
        }
        else if (payment.SupplierId.HasValue)
        {
            await _balanceRecalculation.RecalculateSupplierBalanceAsync(payment.SupplierId.Value, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return true;
    }

    private async Task<string> GeneratePaymentNumberAsync(string prefix, CancellationToken cancellationToken)
    {
        var count = await _context.Payments.CountAsync(cancellationToken) + 1;
        return $"{prefix}-{DateTime.UtcNow:yyyyMMdd}-{count:D4}";
    }
}
