using System.ComponentModel.DataAnnotations;
using VRLCRM.Domain.Entities;

namespace VRLCRM.Models.Customers;

public static class CustomerViewModelMapper
{
    public static CustomerFormViewModel ToFormViewModel(Customer customer)
    {
        return new CustomerFormViewModel
        {
            Id = customer.Id,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            CompanyName = customer.CompanyName,
            PhoneNumber = customer.PhoneNumber,
            Notes = customer.Notes,
            City = customer.Address?.City ?? string.Empty,
            District = customer.Address?.District ?? string.Empty,
            AddressLine = customer.Address?.AddressLine ?? string.Empty,
            CreditLimit = customer.CreditLimit,
            B2bLoginPhone = null
        };
    }

    public static Customer ToCustomer(CustomerFormViewModel model)
    {
        return new Customer
        {
            Id = model.Id,
            FirstName = model.FirstName.Trim(),
            LastName = model.LastName.Trim(),
            CompanyName = model.CompanyName?.Trim(),
            PhoneNumber = model.PhoneNumber.Trim(),
            Notes = model.Notes?.Trim(),
            CreditLimit = model.CreditLimit
        };
    }

    public static Address ToAddress(CustomerFormViewModel model)
    {
        return new Address
        {
            City = model.City.Trim(),
            District = model.District.Trim(),
            AddressLine = model.AddressLine.Trim()
        };
    }
}
