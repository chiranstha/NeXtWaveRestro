using System.ComponentModel.DataAnnotations;

namespace NextWave.Erp.Authorization.Users.Dto;

public class ChangeUserLanguageDto
{
    [Required]
    public string LanguageName { get; set; }
}

