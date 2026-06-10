using Microsoft.AspNetCore.Mvc;
using VRLCRM.Application.Customers;
using VRLCRM.Application.Orders;
using VRLCRM.Application.Stocks;
using VRLCRM.Services;

namespace VRLCRM.Controllers;

public class ShopController : Controller
{
    private readonly ICustomerService _customerService;
    private readonly IStockService _stockService;
    private readonly IOrderService _orderService;
    private readonly CustomerCartService _cartService;

    public ShopController(
        ICustomerService customerService,
        IStockService stockService,
        IOrderService orderService,
        CustomerCartService cartService)
    {
        _customerService = customerService;
        _stockService = stockService;
        _orderService = orderService;
        _cartService = cartService;
    }

    public async Task<IActionResult> Index(int customerId, CancellationToken cancellationToken)
    {
        var customer = await _customerService.GetByIdAsync(customerId, cancellationToken);
        if (customer is null || !customer.IsActive)
        {
            return NotFound();
        }

        var stocks = (await _stockService.GetAllAsync(cancellationToken))
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .ToList();

        ViewBag.Customer = customer;
        ViewBag.CartItems = _cartService.GetItems(customerId);
        ViewBag.CartTotal = _cartService.GetTotal(customerId);

        return View(stocks);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToCart(int customerId, int stockItemId, int quantity, CancellationToken cancellationToken)
    {
        var customer = await _customerService.GetByIdAsync(customerId, cancellationToken);
        if (customer is null || !customer.IsActive)
        {
            return NotFound();
        }

        var stock = await _stockService.GetByIdAsync(stockItemId, cancellationToken);
        if (stock is null || !stock.IsActive)
        {
            TempData["ErrorMessage"] = "Ürün bulunamadı.";
            return RedirectToAction(nameof(Index), new { customerId });
        }

        if (quantity <= 0)
        {
            quantity = 1;
        }

        _cartService.AddItem(customerId, new CartItem
        {
            StockItemId = stock.Id,
            Name = stock.Name,
            UnitPrice = stock.Price,
            Quantity = quantity
        });

        TempData["SuccessMessage"] = $"{stock.Name} sepete eklendi.";
        return RedirectToAction(nameof(Index), new { customerId });
    }

    public async Task<IActionResult> Cart(int customerId, CancellationToken cancellationToken)
    {
        var customer = await _customerService.GetByIdAsync(customerId, cancellationToken);
        if (customer is null || !customer.IsActive)
        {
            return NotFound();
        }

        ViewBag.Customer = customer;
        ViewBag.CartItems = _cartService.GetItems(customerId);
        ViewBag.CartTotal = _cartService.GetTotal(customerId);

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateCart(int customerId, int stockItemId, int quantity)
    {
        _cartService.UpdateQuantity(customerId, stockItemId, quantity);
        return RedirectToAction(nameof(Cart), new { customerId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RemoveFromCart(int customerId, int stockItemId)
    {
        _cartService.RemoveItem(customerId, stockItemId);
        return RedirectToAction(nameof(Cart), new { customerId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(int customerId, string? notes, CancellationToken cancellationToken)
    {
        var customer = await _customerService.GetByIdAsync(customerId, cancellationToken);
        if (customer is null || !customer.IsActive)
        {
            return NotFound();
        }

        var cartItems = _cartService.GetItems(customerId);
        if (cartItems.Count == 0)
        {
            TempData["ErrorMessage"] = "Sepetiniz boş.";
            return RedirectToAction(nameof(Cart), new { customerId });
        }

        try
        {
            var lines = cartItems.Select(i => new OrderLineInput
            {
                StockItemId = i.StockItemId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList();

            var order = await _orderService.CreateAndApproveAsync(customerId, notes, lines, cancellationToken);
            _cartService.Clear(customerId);

            TempData["SuccessMessage"] = $"Sipariş {order.OrderNumber} onaylandı.";
            return RedirectToAction("Details", "Orders", new { id = order.Id });
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Cart), new { customerId });
        }
    }
}
