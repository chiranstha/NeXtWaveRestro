using Abp.Zero.Configuration;
using System.Diagnostics;

namespace Abp.AspNetZeroCore.Licensing
{
    public abstract class AspNetZeroBaseLicenseChecker(IAbpZeroConfig abpZeroConfig)
    {
        private readonly IAbpZeroConfig _abpZeroConfig =
            abpZeroConfig ?? throw new ArgumentNullException(nameof(abpZeroConfig));


        protected bool CompareProjectName(string hashedProjectName) => true;

        protected string GetAssemblyName() =>
            this._abpZeroConfig.EntityTypes.User?.Assembly.GetName().Name ?? string.Empty;

        protected abstract string GetHashedValueWithoutUniqueComputerId(string str);


        protected string GetLicenseController() => "WebProject";

        protected abstract string GetSalt();

        protected bool IsThereAReasonToNotCheck() => !Debugger.IsAttached;
    }
}