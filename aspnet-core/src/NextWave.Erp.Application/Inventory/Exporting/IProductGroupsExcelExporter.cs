using NextWave.Erp.Dto;
using NextWave.Erp.Inventory.Dtos;
using System.Collections.Generic;

namespace NextWave.Erp.Inventory.Exporting
{
    public interface IProductGroupsExcelExporter
    {
        FileDto ExportToFile(List<GetProductGroupForViewDto> productGroups);
    }
}
