using System.Threading.Tasks;
using Abp.Application.Services;
using NextWave.Erp.MultiTenancy.Payments.Dto;
using NextWave.Erp.MultiTenancy.Payments.Stripe.Dto;

namespace NextWave.Erp.MultiTenancy.Payments.Stripe;

public interface IStripePaymentAppService : IApplicationService
{
    Task ConfirmPayment(StripeConfirmPaymentInput input);

    StripeConfigurationDto GetConfiguration();

    Task<string> CreatePaymentSession(StripeCreatePaymentSessionInput input);
}

