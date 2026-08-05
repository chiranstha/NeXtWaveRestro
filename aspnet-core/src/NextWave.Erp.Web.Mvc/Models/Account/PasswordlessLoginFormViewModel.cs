using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace NextWave.Erp.Web.Models.Account;

public class PasswordlessLoginFormViewModel
{
    public bool IsEmailPasswordlessLoginEnabled { get; set; }
    public bool IsSmsPasswordlessLoginEnabled { get; set; }
    public List<SelectListItem> Providers { get; set; }

    public string SelectedProviderValue { get; set; }
    public string SelectedProvider { get; set; }
}

