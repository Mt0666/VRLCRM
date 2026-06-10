using Microsoft.AspNetCore.Mvc;
using VRLCRM.Application.StockMovements;

namespace VRLCRM.Controllers;

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
