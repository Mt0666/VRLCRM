using VRLCRM.Domain.Entities;
using VRLCRM.Domain.Enums;

namespace VRLCRM.Helpers;

public static class PaymentDisplayHelper
{
    public static string GetPaymentTypeLabel(Payment payment)
    {
        if (payment.CustomerId.HasValue)
        {
            return payment.Type == PaymentType.Incoming ? "Müşteriden Tahsilat" : "Müşteriye Ödeme";
        }

        if (payment.SupplierId.HasValue)
        {
            return payment.Type == PaymentType.Incoming ? "Tedarikçiden Tahsilat" : "Tedarikçiye Ödeme";
        }

        return payment.Type == PaymentType.Incoming ? "Tahsilat" : "Ödeme";
    }

    public static string GetPartyName(Payment payment)
    {
        if (payment.CustomerId.HasValue)
        {
            return payment.Customer?.FullName ?? "-";
        }

        return payment.Supplier?.CompanyName ?? "-";
    }
}
