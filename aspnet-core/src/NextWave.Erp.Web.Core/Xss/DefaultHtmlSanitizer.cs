using System.Text.RegularExpressions;

namespace NextWave.Erp.Web.Xss;


public class DefaultHtmlSanitizer : IHtmlSanitizer
{
    public string Sanitize(string html)
    {
        return Regex.Replace(html, "<.*?>|&.*?;", string.Empty);
    }
}

