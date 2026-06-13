using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VRLCRM.Application.StockMovements;
using VRLCRM.Domain.Constants;

namespace VRLCRM.Controllers;

[Authorize(Roles = AppRoles.AdminAndPersonel)]
public class StockMovementsController : Controller
{
    private readonly IStockMovementService _stockMovementService;

    public StockMovementsController(IStockMovementService stockMovementService)
    {
        _stockMovementService = stockMovementService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var movements = await _stockMovementService.GetAllAsync(cancellationToken);
        return View(movements);
    }
}
