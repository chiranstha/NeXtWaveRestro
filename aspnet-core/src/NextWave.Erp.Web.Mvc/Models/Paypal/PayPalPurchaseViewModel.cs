using System.Linq;
using NextWave.Erp.MultiTenancy.Payments.Dto;
using NextWave.Erp.MultiTenancy.Payments.Paypal;

namespace NextWave.Erp.Web.Models.Paypal;

public class PayPalPurchaseViewModel
{
    public SubscriptionPaymentDto Payment { get; set; }

    public decimal Amount { get; set; }

    public PayPalPaymentGatewayConfiguration Configuration { get; set; }

    public string GetDisabledFundingsQueryString()
    {
        if (Configuration.DisabledFundings == null || !Configuration.DisabledFundings.Any())
        {
            return "";
        }

        return "&disable-funding=" + string.Join(',', Configuration.DisabledFundings.ToList());
    }
}

