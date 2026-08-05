namespace NextWave.Erp.Web.Models.Account;

public class LoginFormViewModel
{
    public string SuccessMessage { get; set; }

    public string UserNameOrEmailAddress { get; set; }

    public bool IsSelfRegistrationEnabled { get; set; }

    public bool IsTenantSelfRegistrationEnabled { get; set; }
    
    public bool IsPasswordlessLoginEnabled { get; set; }
    
    public bool IsQrLoginEnabled { get; set; }
}

