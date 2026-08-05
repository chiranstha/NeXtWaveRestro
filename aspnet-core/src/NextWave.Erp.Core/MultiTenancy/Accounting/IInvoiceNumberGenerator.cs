using System.Threading.Tasks;
using Abp.Dependency;

namespace NextWave.Erp.MultiTenancy.Accounting;

public interface IInvoiceNumberGenerator : ITransientDependency
{
    Task<string> GetNewInvoiceNumber();
}

