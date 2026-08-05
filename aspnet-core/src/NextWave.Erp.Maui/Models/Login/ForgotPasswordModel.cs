using System.ComponentModel.DataAnnotations;

namespace NextWave.Erp.Maui.Models.Login;

public class ForgotPasswordModel
{
    [EmailAddress]
    [Required]
    public string EmailAddress { get; set; }
}