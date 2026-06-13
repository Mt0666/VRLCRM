using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VRLCRM.Application.Customers;
using VRLCRM.Domain.Entities;
using VRLCRM.Infrastructure.Data;

namespace VRLCRM.Infrastructure.Customers;

public class CustomerService : ICustomerService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public CustomerService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Customers
            .AsNoTracking()
            .Include(c => c.Address)
            .OrderByDescending(c => c.IsActive)
            .ThenByDescending(c => c.UpdatedAt ?? c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Customer?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Customers
            .Include(c => c.Address)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Customer?> GetByIdWithHistoryAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Customers
            .Include(c => c.Address)
            .Include(c => c.Orders.OrderByDescending(o => o.OrderDate))
            .Include(c => c.Invoices.Where(i => i.InvoiceType == Domain.Enums.InvoiceType.Sales).OrderByDescending(i => i.InvoiceDate))
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Customer> CreateAsync(
        Customer customer,
        Address address,
        string? email = null,
        string? password = null,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        customer.IsActive = true;
        customer.Address = address;
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(password))
        {
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = customer.FullName,
                CustomerId = customer.Id,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Customer");
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"B2B Kullanıcısı oluşturulamadı: {errors}");
            }
        }

        await transaction.CommitAsync(cancellationToken);
        return customer;
    }

    public async Task<IReadOnlyList<Order>> GetOrdersAsync(int customerId, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .AsNoTracking()
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Invoice>> GetSalesInvoicesAsync(int customerId, CancellationToken cancellationToken = default)
    {
        return await _context.Invoices
            .AsNoTracking()
            .Where(i => i.CustomerId == customerId && i.InvoiceType == Domain.Enums.InvoiceType.Sales)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> UpdateAsync(
        Customer customer,
        Address address,
        string? email = null,
        string? password = null,
        CancellationToken cancellationToken = default)
    {
        var existing = await _context.Customers
            .Include(c => c.Address)
            .FirstOrDefaultAsync(c => c.Id == customer.Id, cancellationToken);

        if (existing is null)
        {
            return false;
        }

        existing.FirstName = customer.FirstName;
        existing.LastName = customer.LastName;
        existing.CompanyName = customer.CompanyName;
        existing.PhoneNumber = customer.PhoneNumber;
        existing.Notes = customer.Notes;
        existing.CreditLimit = customer.CreditLimit;

        if (existing.Address is null)
        {
            existing.Address = address;
        }
        else
        {
            existing.Address.City = address.City;
            existing.Address.District = address.District;
            existing.Address.AddressLine = address.AddressLine;
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            var existingUser = await _userManager.Users.FirstOrDefaultAsync(u => u.CustomerId == customer.Id, cancellationToken);
            if (existingUser is null && !string.IsNullOrWhiteSpace(password))
            {
                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FullName = existing.FullName,
                    CustomerId = existing.Id,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Customer");
                }
            }
            else if (existingUser is not null)
            {
                existingUser.Email = email;
                existingUser.UserName = email;
                existingUser.FullName = existing.FullName;
                await _userManager.UpdateAsync(existingUser);

                if (!string.IsNullOrWhiteSpace(password))
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(existingUser);
                    await _userManager.ResetPasswordAsync(existingUser, token, password);
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (customer is null || !customer.IsActive)
            return false;

        customer.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RestoreAsync(int id, CancellationToken cancellationToken = default)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (customer is null || customer.IsActive)
            return false;

        customer.IsActive = true;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
