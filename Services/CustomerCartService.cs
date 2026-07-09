using Microsoft.EntityFrameworkCore;
using VRLCRM.Domain.Entities;
using VRLCRM.Infrastructure.Data;

namespace VRLCRM.Services;

public class CartItem
{
    public int StockItemId { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public string? Notes { get; set; }

    public decimal LineTotal => UnitPrice * Quantity;
}

/// <summary>
/// B2B müşteri sepeti — session yerine VERİTABANINDA saklanır; böylece müşteri
/// günler sonra tekrar girdiğinde (ve uygulama yeniden başlasa bile) sepeti korunur.
/// </summary>
public class CustomerCartService
{
    private readonly ApplicationDbContext _context;

    public CustomerCartService(ApplicationDbContext context)
    {
        _context = context;
    }

    public IReadOnlyList<CartItem> GetItems(int customerId)
    {
        return _context.CartItems
            .AsNoTracking()
            .Where(c => c.CustomerId == customerId)
            .OrderBy(c => c.Id)
            .Select(c => new CartItem
            {
                StockItemId = c.StockItemId,
                Name = c.Name,
                UnitPrice = c.UnitPrice,
                Quantity = c.Quantity,
                Notes = c.Notes
            })
            .ToList();
    }

    public void AddItem(int customerId, CartItem item)
    {
        var existing = _context.CartItems
            .FirstOrDefault(c => c.CustomerId == customerId && c.StockItemId == item.StockItemId);

        if (existing is not null)
        {
            existing.Quantity += item.Quantity;
            existing.UnitPrice = item.UnitPrice;
            existing.Name = item.Name;
        }
        else
        {
            _context.CartItems.Add(new CustomerCartItem
            {
                CustomerId = customerId,
                StockItemId = item.StockItemId,
                Name = item.Name,
                UnitPrice = item.UnitPrice,
                Quantity = item.Quantity,
                Notes = item.Notes
            });
        }

        _context.SaveChanges();
    }

    public void UpdateQuantity(int customerId, int stockItemId, int quantity)
    {
        var existing = _context.CartItems
            .FirstOrDefault(c => c.CustomerId == customerId && c.StockItemId == stockItemId);

        if (existing is null)
        {
            return;
        }

        if (quantity <= 0)
        {
            _context.CartItems.Remove(existing);
        }
        else
        {
            existing.Quantity = quantity;
        }

        _context.SaveChanges();
    }

    public void UpdateNotes(int customerId, int stockItemId, string? notes)
    {
        var existing = _context.CartItems
            .FirstOrDefault(c => c.CustomerId == customerId && c.StockItemId == stockItemId);

        if (existing is null)
        {
            return;
        }

        existing.Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        _context.SaveChanges();
    }

    public void RemoveItem(int customerId, int stockItemId)
    {
        var items = _context.CartItems
            .Where(c => c.CustomerId == customerId && c.StockItemId == stockItemId);

        _context.CartItems.RemoveRange(items);
        _context.SaveChanges();
    }

    public void Clear(int customerId)
    {
        var items = _context.CartItems.Where(c => c.CustomerId == customerId);
        _context.CartItems.RemoveRange(items);
        _context.SaveChanges();
    }

    public decimal GetTotal(int customerId)
    {
        return _context.CartItems
            .Where(c => c.CustomerId == customerId)
            .Sum(c => (decimal?)(c.UnitPrice * c.Quantity)) ?? 0m;
    }
}
