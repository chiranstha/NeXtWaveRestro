using Abp.Dependency;
using Abp.Extensions;
using Abp.Runtime.Session;
using NextWave.Erp.Editions;
using NextWave.Erp.ExtraProperties;
using NextWave.Erp.MultiTenancy.Payments;
using NextWave.Erp.Url;

namespace NextWave.Erp.Web.Url;

public class PaymentUrlGenerator : IPaymentUrlGenerator, ITransientDependency
{
    private readonly IWebUrlService _webUrlService;

    public PaymentUrlGenerator(
        IWebUrlService webUrlService)
    {
        _webUrlService = webUrlService;
    }

    public string CreatePaymentRequestUrl(SubscriptionPayment subscriptionPayment)
    {
        var webSiteRootAddress = _webUrlService.GetSiteRootAddress();

        var url = webSiteRootAddress.EnsureEndsWith('/') +
                  "Payment/GatewaySelection" +
                  "?paymentId=" + subscriptionPayment.Id;

        return url;
    }
}

