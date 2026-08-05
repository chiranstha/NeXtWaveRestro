using System.ComponentModel.DataAnnotations;

namespace NextWave.Erp.Localization.Dto;

public class CreateOrUpdateLanguageInput
{
    [Required]
    public ApplicationLanguageEditDto Language { get; set; }
}

