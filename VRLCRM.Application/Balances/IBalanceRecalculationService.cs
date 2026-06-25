namespace VRLCRM.Application.Balances;

public interface IBalanceRecalculationService
{
    Task RecalculateCustomerBalanceAsync(int customerId, CancellationToken cancellationToken = default);

    Task RecalculateSupplierBalanceAsync(int supplierId, CancellationToken cancellationToken = default);

    Task RecalculateAllAsync(CancellationToken cancellationToken = default);
}
