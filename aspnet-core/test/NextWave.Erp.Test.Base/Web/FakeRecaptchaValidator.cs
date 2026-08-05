using System.Threading.Tasks;
using NextWave.Erp.Security.Recaptcha;

namespace NextWave.Erp.Test.Base.Web
{
    public class FakeRecaptchaValidator : IRecaptchaValidator
    {
        public Task ValidateAsync(string captchaResponse)
        {
            return Task.CompletedTask;
        }
    }
}
