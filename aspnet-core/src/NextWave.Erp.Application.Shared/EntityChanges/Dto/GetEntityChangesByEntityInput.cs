using NextWave.Erp.Dto;
using System;

namespace NextWave.Erp.EntityChanges.Dto;

public class GetEntityChangesByEntityInput
{
    public string EntityTypeFullName { get; set; }
    public string EntityId { get; set; }
}

