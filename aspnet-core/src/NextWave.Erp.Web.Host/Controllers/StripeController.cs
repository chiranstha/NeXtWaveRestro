using NextWave.Erp.MultiTenancy.Payments.Stripe;

namespace NextWave.Erp.Web.Controllers;

public class StripeController : StripeControllerBase
{
    public StripeController(
        StripeGatewayManager stripeGatewayManager,
        StripePaymentGatewayConfiguration stripeConfiguration,
        IStripePaymentAppService stripePaymentAppService)
        : base(stripeGatewayManager, stripeConfiguration, stripePaymentAppService)
    {
    }
}

