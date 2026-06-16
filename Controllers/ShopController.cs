using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VRLCRM.Application.Customers;
using VRLCRM.Application.Orders;
using VRLCRM.Application.Stocks;
using VRLCRM.Domain.Entities;
using VRLCRM.Services;

namespace VRLCRM.Controllers;

[Authorize(Roles = "Customer")]
public class ShopController : Controller
{
    private readonly ICustomerService _customerService;
    private readonly IStockService _stockService;
    private readonly IOrderService _orderService;
    private readonly CustomerCartService _cartService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ShopController(
        ICustomerService customerService,
        IStockService stockService,
        IOrderService orderService,
        CustomerCartService cartService,
        UserManager<ApplicationUser> userManager)
    {
        _customerService = customerService;
        _stockService = stockService;
        _orderService = orderService;
        _cartService = cartService;
        _userManager = userManager;
    }

    private async Task<(Customer? customer, IActionResult? error)> GetCurrentCustomerAsync(CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user?.CustomerId is null)
            return (null, Forbid());

        var customer = await _customerService.GetByIdAsync(user.CustomerId.Value, cancellationToken);
        if (customer is null || !customer.IsActive)
            return (null, Forbid());

        return (customer, null);
    }

    private void SetCartViewBag(int customerId)
    {
        ViewBag.CartCount = _cartService.GetItems(customerId).Count;
        ViewBag.CartTotal = _cartService.GetTotal(customerId);
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var (customer, error) = await GetCurrentCustomerAsync(cancellationToken);
        if (error is not null) return error;

        var stocks = (await _stockService.GetAllAsync(cancellationToken))
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .ToList();

        ViewBag.Customer = customer;
        SetCartViewBag(customer!.Id);

        return View(stocks);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToCart(int stockItemId, int quantity, CancellationToken cancellationToken)
    {
        var (customer, error) = await GetCurrentCustomerAsync(cancellationToken);
        if (error is not null) return error;

        var stock = await _stockService.GetByIdAsync(stockItemId, cancellationToken);
        if (stock is null || !stock.IsActive)
        {
            TempData["ErrorMessage"] = "Ürün bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        if (quantity <= 0) quantity = 1;

        _cartService.AddItem(customer!.Id, new CartItem
        {
            StockItemId = stock.Id,
            Name = stock.Name,
            UnitPrice = stock.Price,
            Quantity = quantity
        });

        TempData["SuccessMessage"] = $"{stock.Name} sepete eklendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToCartAjax(int stockItemId, int quantity, CancellationToken cancellationToken)
    {
        var (customer, error) = await GetCurrentCustomerAsync(cancellationToken);
        if (error is not null) return Json(new { success = false, message = "Yetki hatası." });

        var stock = await _stockService.GetByIdAsync(stockItemId, cancellationToken);
        if (stock is null || !stock.IsActive)
            return Json(new { success = false, message = "Ürün bulunamadı." });

        if (quantity <= 0) quantity = 1;

        _cartService.AddItem(customer!.Id, new CartItem
        {
            StockItemId = stock.Id,
            Name = stock.Name,
            UnitPrice = stock.Price,
            Quantity = quantity
        });

        var cartCount = _cartService.GetItems(customer.Id).Count;
        var cartTotal = _cartService.GetTotal(customer.Id);
        return Json(new { success = true, message = $"{stock.Name} sepete eklendi.", cartCount, cartTotal = cartTotal.ToString("N2") });
    }

    public async Task<IActionResult> Cart(CancellationToken cancellationToken)
    {
        var (customer, error) = await GetCurrentCustomerAsync(cancellationToken);
        if (error is not null) return error;

        ViewBag.Customer = customer;
        ViewBag.CartItems = _cartService.GetItems(customer!.Id);
        ViewBag.CartTotal = _cartService.GetTotal(customer.Id);
        SetCartViewBag(customer.Id);

        return View();
    }

    public async Task<IActionResult> Orders(CancellationToken cancellationToken)
    {
        var (customer, error) = await GetCurrentCustomerAsync(cancellationToken);
        if (error is not null) return error;

        var orders = await _customerService.GetOrdersAsync(customer!.Id, cancellationToken);
        ViewBag.Customer = customer;
        SetCartViewBag(customer.Id);

        return View(orders);
    }

    public async Task<IActionResult> OrderDetails(int id, CancellationToken cancellationToken)
    {
        var (customer, error) = await GetCurrentCustomerAsync(cancellationToken);
        if (error is not null) return error;

        var order = await _orderService.GetByIdAsync(id, cancellationToken);
        if (order is null || order.CustomerId != customer!.Id)
        {
            return NotFound();
        }

        ViewBag.Customer = customer;
        SetCartViewBag(customer.Id);

        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateCart(int stockItemId, int quantity, CancellationToken cancellationToken)
    {
        var (customer, error) = await GetCurrentCustomerAsync(cancellationToken);
        if (error is not null) return error;

        _cartService.UpdateQuantity(customer!.Id, stockItemId, quantity);
        return RedirectToAction(nameof(Cart));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateCartNotes(int stockItemId, string? notes, CancellationToken cancellationToken)
    {
        var (customer, error) = await GetCurrentCustomerAsync(cancellationToken);
        if (error is not null) return error;

        _cartService.UpdateNotes(customer!.Id, stockItemId, notes);
        return RedirectToAction(nameof(Cart));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveFromCart(int stockItemId, CancellationToken cancellationToken)
    {
        var (customer, error) = await GetCurrentCustomerAsync(cancellationToken);
        if (error is not null) return error;

        _cartService.RemoveItem(customer!.Id, stockItemId);
        return RedirectToAction(nameof(Cart));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(string? notes, CancellationToken cancellationToken)
    {
        var (customer, error) = await GetCurrentCustomerAsync(cancellationToken);
        if (error is not null) return error;

        var cartItems = _cartService.GetItems(customer!.Id);
        if (cartItems.Count == 0)
        {
            TempData["ErrorMessage"] = "Sepetiniz boş.";
            return RedirectToAction(nameof(Cart));
        }

        try
        {
            var lines = cartItems.Select(i => new OrderLineInput
            {
                StockItemId = i.StockItemId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                Notes = i.Notes
            }).ToList();

            var order = await _orderService.CreateAndApproveAsync(customer.Id, notes, 0, lines, cancellationToken);
            _cartService.Clear(customer.Id);

            return RedirectToAction(nameof(OrderConfirmation), new { orderNumber = order.OrderNumber, total = order.SubTotal });
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Cart));
        }
    }

    public IActionResult OrderConfirmation(string orderNumber, decimal total)
    {
        ViewBag.OrderNumber = orderNumber;
        ViewBag.Total = total;
        return View();
    }
}
