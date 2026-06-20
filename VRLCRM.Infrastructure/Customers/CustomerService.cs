using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VRLCRM.Application.Customers;
using VRLCRM.Domain.Entities;
using VRLCRM.Domain.Enums;
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
        string? loginPhone = null,
        string? password = null,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        customer.IsActive = true;
        customer.CreditLimit = customer.CreditLimit ?? 0;
        customer.Address = address;
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync(cancellationToken);

        var phone = !string.IsNullOrWhiteSpace(loginPhone) ? loginPhone : customer.PhoneNumber;
        if (!string.IsNullOrWhiteSpace(phone) && !string.IsNullOrWhiteSpace(password))
        {
            var userName = NormalizePhone(phone);
            var user = new ApplicationUser
            {
                UserName = userName,
                Email = $"{userName}@b2b.local",
                FullName = customer.FullName,
                CustomerId = customer.Id,
                EmailConfirmed = true,
                PhoneNumber = customer.PhoneNumber
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

    public async Task<IReadOnlyList<Payment>> GetIncomingPaymentsAsync(int customerId, CancellationToken cancellationToken = default)
    {
        return await _context.Payments
            .AsNoTracking()
            .Where(p => p.CustomerId == customerId &&
                        p.Type == PaymentType.Incoming &&
                        p.IsActive)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync(cancellationToken);
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
        existing.CreditLimit = customer.CreditLimit ?? 0;

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
        if (!string.IsNullOrWhiteSpace(password) || existingUser is not null)
        {
            var phone = !string.IsNullOrWhiteSpace(loginPhone) ? loginPhone : existing.PhoneNumber;
            var userName = NormalizePhone(phone);
            if (string.IsNullOrWhiteSpace(userName))
            {
                throw new InvalidOperationException("B2B giriş telefonu geçerli bir numara olmalıdır.");
            }

            if (existingUser is null)
            {
                if (string.IsNullOrWhiteSpace(password))
                {
                    // B2B hesabı oluşturulmadı; yalnızca müşteri bilgileri güncellendi.
                }
                else
                {
                    var user = new ApplicationUser
                    {
                        UserName = userName,
                        Email = $"{userName}@b2b.local",
                        FullName = existing.FullName,
                        CustomerId = existing.Id,
                        EmailConfirmed = true,
                        PhoneNumber = existing.PhoneNumber
                    };

                    var result = await _userManager.CreateAsync(user, password);
                    if (!result.Succeeded)
                    {
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        throw new InvalidOperationException($"B2B kullanıcısı oluşturulamadı: {errors}");
                    }

                    await _userManager.AddToRoleAsync(user, "Customer");
                }
            }
            else
            {
                existingUser.UserName = userName;
                existingUser.Email = $"{userName}@b2b.local";
                existingUser.FullName = existing.FullName;
                existingUser.PhoneNumber = existing.PhoneNumber;
                var updateResult = await _userManager.UpdateAsync(existingUser);
                if (!updateResult.Succeeded)
                {
                    var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"B2B kullanıcısı güncellenemedi: {errors}");
                }

                if (!string.IsNullOrWhiteSpace(password))
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(existingUser);
                    var resetResult = await _userManager.ResetPasswordAsync(existingUser, token, password);
                    if (!resetResult.Succeeded)
                    {
                        var errors = string.Join(", ", resetResult.Errors.Select(e => e.Description));
                        throw new InvalidOperationException($"B2B şifresi güncellenemedi: {errors}");
                    }
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

    private static string NormalizePhone(string phone) =>
        new string(phone.Where(char.IsDigit).ToArray());
}
