using Abp.Dependency;

namespace NextWave.Erp.Web.Xss;

public interface IHtmlSanitizer : ITransientDependency
{
    string Sanitize(string html);
}

