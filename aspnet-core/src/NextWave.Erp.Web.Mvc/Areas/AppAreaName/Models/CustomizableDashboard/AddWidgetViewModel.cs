using System.Collections.Generic;
using NextWave.Erp.DashboardCustomization.Dto;

namespace NextWave.Erp.Web.Areas.AppAreaName.Models.CustomizableDashboard;

public class AddWidgetViewModel
{
    public List<WidgetOutput> Widgets { get; set; }

    public string DashboardName { get; set; }

    public string PageId { get; set; }
}

