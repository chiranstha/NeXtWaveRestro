using System.Threading.Tasks;

namespace NextWave.Erp.Security.Recaptcha;

public interface IRecaptchaValidator
{
    Task ValidateAsync(string captchaResponse);
}
