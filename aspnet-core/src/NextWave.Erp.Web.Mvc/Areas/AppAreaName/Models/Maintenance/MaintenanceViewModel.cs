using System.Collections.Generic;
using NextWave.Erp.Caching.Dto;

namespace NextWave.Erp.Web.Areas.AppAreaName.Models.Maintenance;

public class MaintenanceViewModel
{
    public IReadOnlyList<CacheDto> Caches { get; set; }

    public bool CanClearAllCaches { get; set; }
}

