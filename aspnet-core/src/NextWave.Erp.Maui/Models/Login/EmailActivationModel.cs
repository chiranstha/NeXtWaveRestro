using System.ComponentModel.DataAnnotations;

namespace NextWave.Erp.Maui.Models.Login;

public class EmailActivationModel
{
    [Required]
    [EmailAddress]
    public string EmailAddress { get; set; }
}