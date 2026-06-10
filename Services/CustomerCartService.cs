using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace VRLCRM.Services;

public class CartItem
{
    public int StockItemId { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public decimal LineTotal => UnitPrice * Quantity;
}

public class CustomerCartService
{
    private const string SessionPrefix = "cart_";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CustomerCartService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public IReadOnlyList<CartItem> GetItems(int customerId)
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        if (session is null)
        {
            return [];
        }

        var json = session.GetString(SessionKey(customerId));
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<CartItem>>(json) ?? [];
    }

    public void SaveItems(int customerId, List<CartItem> items)
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        if (session is null)
        {
            return;
        }

        session.SetString(SessionKey(customerId), JsonSerializer.Serialize(items));
    }

    public void AddItem(int customerId, CartItem item)
    {
        var items = GetItems(customerId).ToList();
        var existing = items.FirstOrDefault(i => i.StockItemId == item.StockItemId);
        if (existing is not null)
        {
            existing.Quantity += item.Quantity;
        }
        else
        {
            items.Add(item);
        }

        SaveItems(customerId, items);
    }

    public void UpdateQuantity(int customerId, int stockItemId, int quantity)
    {
        var items = GetItems(customerId).ToList();
        var existing = items.FirstOrDefault(i => i.StockItemId == stockItemId);
        if (existing is null)
        {
            return;
        }

        if (quantity <= 0)
        {
            items.Remove(existing);
        }
        else
        {
            existing.Quantity = quantity;
        }

        SaveItems(customerId, items);
    }

    public void RemoveItem(int customerId, int stockItemId)
    {
        var items = GetItems(customerId).ToList();
        items.RemoveAll(i => i.StockItemId == stockItemId);
        SaveItems(customerId, items);
    }

    public void Clear(int customerId)
    {
        SaveItems(customerId, []);
    }

    public decimal GetTotal(int customerId)
    {
        return GetItems(customerId).Sum(i => i.LineTotal);
    }

    private static string SessionKey(int customerId) => $"{SessionPrefix}{customerId}";
}
