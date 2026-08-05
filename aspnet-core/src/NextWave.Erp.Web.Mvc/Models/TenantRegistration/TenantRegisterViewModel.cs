using NextWave.Erp.Editions;
using NextWave.Erp.Editions.Dto;
using NextWave.Erp.MultiTenancy.Payments;
using NextWave.Erp.Security;
using NextWave.Erp.MultiTenancy.Payments.Dto;

namespace NextWave.Erp.Web.Models.TenantRegistration;

public class TenantRegisterViewModel
{
    public int? EditionId { get; set; }

    public EditionSelectDto Edition { get; set; }

    public PasswordComplexitySetting PasswordComplexitySetting { get; set; }

    public EditionPaymentType EditionPaymentType { get; set; }

    public SubscriptionStartType? SubscriptionStartType { get; set; }

    public PaymentPeriodType? PaymentPeriodType { get; set; }

    public string SuccessUrl { get; set; }

    public string ErrorUrl { get; set; }
}

