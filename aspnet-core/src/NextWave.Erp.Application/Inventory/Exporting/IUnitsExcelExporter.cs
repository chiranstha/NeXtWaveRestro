using NextWave.Erp.Dto;
using NextWave.Erp.Inventory.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Inventory.Exporting
{
    public interface IUnitsExcelExporter
    {
        FileDto ExportToFile(List<GetUnitForViewDto> units);
    }
}
