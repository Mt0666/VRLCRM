using VRLCRM.Domain.Entities;
using VRLCRM.Domain.Enums;

namespace VRLCRM.Application.Payments;

public interface IPaymentService
{
    Task<IReadOnlyList<Payment>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Payment?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Payment> CreateIncomingPaymentAsync(int customerId, decimal amount, PaymentMethod method, DateTime paymentDate, string? notes, CancellationToken cancellationToken = default);

    Task<Payment> CreateOutgoingPaymentToCustomerAsync(int customerId, decimal amount, PaymentMethod method, DateTime paymentDate, string? notes, CancellationToken cancellationToken = default);

    Task<Payment> CreateOutgoingPaymentAsync(int supplierId, decimal amount, PaymentMethod method, DateTime paymentDate, string? notes, CancellationToken cancellationToken = default);

    Task<Payment> CreateIncomingPaymentFromSupplierAsync(int supplierId, decimal amount, PaymentMethod method, DateTime paymentDate, string? notes, CancellationToken cancellationToken = default);
    Task<bool> DeactivateAsync(int id, CancellationToken cancellationToken = default);
}
