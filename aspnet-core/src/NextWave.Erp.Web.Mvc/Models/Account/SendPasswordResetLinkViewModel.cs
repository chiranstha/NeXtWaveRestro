using System.ComponentModel.DataAnnotations;

namespace NextWave.Erp.Web.Models.Account;

public class SendPasswordResetLinkViewModel
{
    [Required]
    public string EmailAddress { get; set; }
}

