using System.Collections.Generic;
using System.Linq;
using NextWave.Erp.MultiTenancy.Payments;
using NextWave.Erp.MultiTenancy.Payments.Dto;

namespace NextWave.Erp.Web.Models.Payment;

public class GatewaySelectionViewModel
{
    public SubscriptionPaymentDto Payment { get; set; }

    public List<PaymentGatewayModel> PaymentGateways { get; set; }

    public bool AllowRecurringPaymentOption()
    {
        return Payment.AllowRecurringPayment() && PaymentGateways.Any(gateway => gateway.SupportsRecurringPayments);
    }
}

