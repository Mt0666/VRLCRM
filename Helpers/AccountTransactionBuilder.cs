using VRLCRM.Domain.Entities;
using VRLCRM.Domain.Enums;
using VRLCRM.Models.Accounts;

namespace VRLCRM.Helpers;

public static class AccountTransactionBuilder
{
    public static IEnumerable<AccountTransactionRow> FromCustomerData(
        IReadOnlyList<Order> orders,
        IReadOnlyList<Invoice> salesInvoices,
        IReadOnlyList<Payment> payments)
    {
        var linkedInvoiceIds = orders
            .Where(o => o.SalesInvoiceId.HasValue)
            .Select(o => o.SalesInvoiceId!.Value)
            .ToHashSet();

        var rows = new List<AccountTransactionRow>();

        foreach (var order in orders)
        {
            rows.Add(new AccountTransactionRow
            {
                Id = order.Id,
                Number = order.OrderNumber,
                Date = order.OrderDate,
                Kind = AccountTransactionKind.Order,
                TypeLabel = "Sipariş",
                TypeBadgeColor = "primary",
                StatusLabel = order.StatusLabel,
                StatusColor = order.Status switch
                {
                    OrderStatus.Approved => "success",
                    OrderStatus.Cancelled => "danger",
                    _ => "secondary"
                },
                Amount = order.TotalAmount,
                BalanceEffectLabel = order.SalesInvoiceId.HasValue ? "Borç +" : "—",
                BalanceEffectColor = order.SalesInvoiceId.HasValue ? "danger" : "secondary",
                DetailController = "Orders"
            });
        }

        foreach (var invoice in salesInvoices.Where(i => i.IsActive && !linkedInvoiceIds.Contains(i.Id)))
        {
            rows.Add(new AccountTransactionRow
            {
                Id = invoice.Id,
                Number = invoice.InvoiceNumber,
                Date = invoice.InvoiceDate,
                Kind = AccountTransactionKind.SalesInvoice,
                TypeLabel = "Satış Faturası",
                TypeBadgeColor = "warning",
                StatusLabel = "Aktif",
                StatusColor = "success",
                Amount = invoice.TotalAmount,
                BalanceEffectLabel = "Borç +",
                BalanceEffectColor = "danger",
                DetailController = "Invoices"
            });
        }

        foreach (var payment in payments)
        {
            var isIncoming = payment.Type == PaymentType.Incoming;
            rows.Add(new AccountTransactionRow
            {
                Id = payment.Id,
                Number = payment.PaymentNumber,
                Date = payment.PaymentDate,
                Kind = isIncoming ? AccountTransactionKind.PaymentIncoming : AccountTransactionKind.PaymentOutgoing,
                TypeLabel = PaymentDisplayHelper.GetPaymentTypeLabel(payment),
                TypeBadgeColor = isIncoming ? "success" : "danger",
                StatusLabel = "Aktif",
                StatusColor = "success",
                Amount = payment.Amount,
                BalanceEffectLabel = isIncoming ? "Borç −" : "Borç +",
                BalanceEffectColor = isIncoming ? "success" : "danger",
                DetailController = "Payments"
            });
        }

        return rows.OrderByDescending(r => r.Date);
    }

    public static IEnumerable<AccountTransactionRow> FromSupplierData(
        IReadOnlyList<Order> orders,
        IReadOnlyList<Invoice> purchaseInvoices,
        IReadOnlyList<Invoice> salesInvoices,
        IReadOnlyList<Payment> payments)
    {
        var linkedInvoiceIds = orders
            .Where(o => o.SalesInvoiceId.HasValue)
            .Select(o => o.SalesInvoiceId!.Value)
            .ToHashSet();

        var rows = new List<AccountTransactionRow>();

        foreach (var order in orders)
        {
            rows.Add(new AccountTransactionRow
            {
                Id = order.Id,
                Number = order.OrderNumber,
                Date = order.OrderDate,
                Kind = AccountTransactionKind.Order,
                TypeLabel = "Sipariş",
                TypeBadgeColor = "primary",
                StatusLabel = order.StatusLabel,
                StatusColor = order.Status switch
                {
                    OrderStatus.Approved => "success",
                    OrderStatus.Cancelled => "danger",
                    _ => "secondary"
                },
                Amount = order.TotalAmount,
                BalanceEffectLabel = order.SalesInvoiceId.HasValue ? "Borç −" : "—",
                BalanceEffectColor = order.SalesInvoiceId.HasValue ? "success" : "secondary",
                DetailController = "Orders"
            });
        }

        foreach (var invoice in purchaseInvoices.Where(i => i.IsActive))
        {
            rows.Add(new AccountTransactionRow
            {
                Id = invoice.Id,
                Number = invoice.InvoiceNumber,
                Date = invoice.InvoiceDate,
                Kind = AccountTransactionKind.PurchaseInvoice,
                TypeLabel = "Alış Faturası",
                TypeBadgeColor = "warning",
                StatusLabel = "Aktif",
                StatusColor = "success",
                Amount = invoice.TotalAmount,
                BalanceEffectLabel = "Borç +",
                BalanceEffectColor = "danger",
                DetailController = "Invoices"
            });
        }

        foreach (var invoice in salesInvoices.Where(i => i.IsActive && !linkedInvoiceIds.Contains(i.Id)))
        {
            rows.Add(new AccountTransactionRow
            {
                Id = invoice.Id,
                Number = invoice.InvoiceNumber,
                Date = invoice.InvoiceDate,
                Kind = AccountTransactionKind.SalesInvoice,
                TypeLabel = "Satış Faturası",
                TypeBadgeColor = "info",
                StatusLabel = "Aktif",
                StatusColor = "success",
                Amount = invoice.TotalAmount,
                BalanceEffectLabel = "Borç −",
                BalanceEffectColor = "success",
                DetailController = "Invoices"
            });
        }

        foreach (var payment in payments)
        {
            var isIncoming = payment.Type == PaymentType.Incoming;
            rows.Add(new AccountTransactionRow
            {
                Id = payment.Id,
                Number = payment.PaymentNumber,
                Date = payment.PaymentDate,
                Kind = isIncoming ? AccountTransactionKind.PaymentIncoming : AccountTransactionKind.PaymentOutgoing,
                TypeLabel = PaymentDisplayHelper.GetPaymentTypeLabel(payment),
                TypeBadgeColor = isIncoming ? "success" : "danger",
                StatusLabel = "Aktif",
                StatusColor = "success",
                Amount = payment.Amount,
                BalanceEffectLabel = isIncoming ? "Borç +" : "Borç −",
                BalanceEffectColor = isIncoming ? "danger" : "success",
                DetailController = "Payments"
            });
        }

        return rows.OrderByDescending(r => r.Date);
    }
}
