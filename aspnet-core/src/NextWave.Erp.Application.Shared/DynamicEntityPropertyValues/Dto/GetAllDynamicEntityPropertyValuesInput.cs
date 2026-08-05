using System.ComponentModel.DataAnnotations;

namespace NextWave.Erp.DynamicEntityPropertyValues.Dto;

public class GetAllDynamicEntityPropertyValuesInput
{
    [Required]
    public string EntityFullName { get; set; }

    [Required]
    public string EntityId { get; set; }
}

