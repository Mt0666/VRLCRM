using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VRLCRM.Application.Common;
using VRLCRM.Application.Customers;
using VRLCRM.Domain.Entities;
using VRLCRM.Domain.Enums;
using VRLCRM.Infrastructure.Data;

namespace VRLCRM.Infrastructure.Customers;

public class CustomerService : ICustomerService
{
    private const string CustomerRole = "Customer";

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
        string? loginPhone = null,
        string? password = null,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        customer.IsActive = true;
        customer.Address = address;
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync(cancellationToken);

        var phone = !string.IsNullOrWhiteSpace(loginPhone) ? loginPhone : customer.PhoneNumber;
        if (!string.IsNullOrWhiteSpace(phone) && !string.IsNullOrWhiteSpace(password))
        {
            await CreateB2bUserAsync(customer, phone, password, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return customer;
    }

    public async Task<IReadOnlyList<Order>> GetOrdersAsync(int customerId, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .AsNoTracking()
            .Where(o => o.CustomerId == customerId && o.IsActive && o.Status != OrderStatus.Cancelled)
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

    public async Task<IReadOnlyList<Payment>> GetPaymentsAsync(int customerId, CancellationToken cancellationToken = default)
    {
        return await _context.Payments
            .AsNoTracking()
            .Where(p => p.CustomerId == customerId && p.IsActive)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<string?> GetB2bLoginPhoneAsync(int customerId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.CustomerId == customerId, cancellationToken);

        return user?.UserName;
    }

    public async Task<bool> UpdateAsync(
        Customer customer,
        Address address,
        string? loginPhone = null,
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

        var existingUser = await _userManager.Users.FirstOrDefaultAsync(u => u.CustomerId == customer.Id, cancellationToken);
        await SyncB2bUserAsync(existing, existingUser, loginPhone, password, cancellationToken);

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

    private async Task CreateB2bUserAsync(
        Customer customer,
        string loginPhone,
        string password,
        CancellationToken cancellationToken)
    {
        var userName = PhoneNormalizer.Normalize(loginPhone);
        if (string.IsNullOrWhiteSpace(userName))
        {
            throw new InvalidOperationException("B2B giriş telefonu geçerli bir numara olmalıdır.");
        }

        var user = new ApplicationUser
        {
            UserName = userName,
            Email = userName,
            FullName = customer.FullName,
            CustomerId = customer.Id,
            EmailConfirmed = true,
            PhoneNumber = userName
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"B2B kullanıcısı oluşturulamadı: {errors}");
        }

        await _userManager.AddToRoleAsync(user, CustomerRole);
    }

    private async Task SyncB2bUserAsync(
        Customer existing,
        ApplicationUser? existingUser,
        string? loginPhone,
        string? password,
        CancellationToken cancellationToken)
    {
        var phone = !string.IsNullOrWhiteSpace(loginPhone) ? loginPhone : existing.PhoneNumber;
        var userName = PhoneNormalizer.Normalize(phone);

        if (string.IsNullOrWhiteSpace(userName))
        {
            if (!string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException("B2B giriş telefonu geçerli bir numara olmalıdır.");
            }

            return;
        }

        if (existingUser is null)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                return;
            }

            await CreateB2bUserAsync(existing, phone, password, cancellationToken);
            return;
        }

        if (!string.Equals(existingUser.UserName, userName, StringComparison.Ordinal))
        {
            var nameResult = await _userManager.SetUserNameAsync(existingUser, userName);
            if (!nameResult.Succeeded)
            {
                var errors = string.Join(", ", nameResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"B2B kullanıcı adı güncellenemedi: {errors}");
            }
        }

        if (!string.Equals(existingUser.Email, userName, StringComparison.Ordinal))
        {
            var emailResult = await _userManager.SetEmailAsync(existingUser, userName);
            if (!emailResult.Succeeded)
            {
                var errors = string.Join(", ", emailResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"B2B e-posta güncellenemedi: {errors}");
            }
        }

        existingUser.FullName = existing.FullName;
        existingUser.PhoneNumber = userName;
        var updateResult = await _userManager.UpdateAsync(existingUser);
        if (!updateResult.Succeeded)
        {
            var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"B2B kullanıcısı güncellenemedi: {errors}");
        }

        if (!await _userManager.IsInRoleAsync(existingUser, CustomerRole))
        {
            await _userManager.AddToRoleAsync(existingUser, CustomerRole);
        }

        if (!string.IsNullOrWhiteSpace(password))
        {
            var freshUser = await _userManager.FindByIdAsync(existingUser.Id)
                ?? throw new InvalidOperationException("B2B kullanıcısı bulunamadı.");

            var token = await _userManager.GeneratePasswordResetTokenAsync(freshUser);
            var resetResult = await _userManager.ResetPasswordAsync(freshUser, token, password);
            if (!resetResult.Succeeded)
            {
                var addResult = await _userManager.AddPasswordAsync(freshUser, password);
                if (!addResult.Succeeded)
                {
                    var errors = string.Join(", ", resetResult.Errors.Concat(addResult.Errors).Select(e => e.Description).Distinct());
                    throw new InvalidOperationException($"B2B şifresi güncellenemedi: {errors}");
                }
            }
        }
    }
}
