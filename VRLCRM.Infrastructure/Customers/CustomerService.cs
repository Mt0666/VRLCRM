using Microsoft.EntityFrameworkCore;
using VRLCRM.Application.Customers;
using VRLCRM.Domain.Entities;
using VRLCRM.Infrastructure.Data;

namespace VRLCRM.Infrastructure.Customers;

public class CustomerService : ICustomerService
{
    private readonly ApplicationDbContext _context;

    public CustomerService(ApplicationDbContext context)
    {
        _context = context;
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

    public async Task<Customer> CreateAsync(
        Customer customer,
        Address address,
        CancellationToken cancellationToken = default)
    {
        customer.IsActive = true;
        customer.Address = address;
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync(cancellationToken);
        return customer;
    }

    public async Task<bool> UpdateAsync(
        Customer customer,
        Address address,
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

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (customer is null)
        {
            return false;
        }

        if (!customer.IsActive)
        {
            return false;
        }

        customer.IsActive = false;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
