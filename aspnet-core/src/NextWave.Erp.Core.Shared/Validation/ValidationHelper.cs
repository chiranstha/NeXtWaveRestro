using System.Text.RegularExpressions;
using Abp.Extensions;

namespace NextWave.Erp.Validation;

public static class ValidationHelper
{
    public const string EmailRegex = @"^\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$";

    public static bool IsEmail(string value)
    {
        if (value.IsNullOrEmpty())
        {
            return false;
        }

        var regex = new Regex(EmailRegex);
        return regex.IsMatch(value);
    }

    public static bool IsValidNepaliDate(string value)
    {
        //^[+-]?\d{4}/([1-9]|1[0-2])/(0[1-9]|[12][0-9]|3[02])$
        Regex regex = new Regex(
            @"(((19|20)([2468][048]|[13579][26]|0[48])|2000)[/]02[/]29|((19|20)[0-9]{2}[/](0[4678]|1[02])[/](0[1-9]|[12][0-9]|30)|(19|20)[0-9]{2}[/](0[1359]|11)[/](0[1-9]|[12][0-9]|3[01])|(19|20)[0-9]{2}[/]02[/](0[1-9]|1[0-9]|2[0-8])))");
        bool isValid = regex.IsMatch(value.Trim());
        return isValid;
    }


}

