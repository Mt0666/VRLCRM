using VRLCRM.Domain.Entities;

namespace VRLCRM.Models.Suppliers;

public static class SupplierViewModelMapper
{
    public static SupplierFormViewModel ToFormViewModel(Supplier supplier)
    {
        return new SupplierFormViewModel
        {
            Id = supplier.Id,
            CompanyName = supplier.CompanyName,
            ContactName = supplier.ContactName,
            PhoneNumber = supplier.PhoneNumber,
            TaxNumber = supplier.TaxNumber,
            Notes = supplier.Notes,
            CreditLimit = supplier.CreditLimit,
            City = supplier.City,
            District = supplier.District,
            AddressLine = supplier.AddressLine
        };
    }

    public static Supplier ToSupplier(SupplierFormViewModel model)
    {
        return new Supplier
        {
            Id = model.Id,
            CompanyName = model.CompanyName.Trim(),
            ContactName = model.ContactName?.Trim(),
            PhoneNumber = model.PhoneNumber.Trim(),
            TaxNumber = model.TaxNumber?.Trim(),
            Notes = model.Notes?.Trim(),
            CreditLimit = model.CreditLimit,
            City = model.City?.Trim(),
            District = model.District?.Trim(),
            AddressLine = model.AddressLine?.Trim()
        };
    }
}
