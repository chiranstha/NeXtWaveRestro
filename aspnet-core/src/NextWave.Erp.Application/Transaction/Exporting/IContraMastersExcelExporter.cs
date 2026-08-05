using NextWave.Erp.Dto;
using NextWave.Erp.Transaction.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Transaction.Exporting
{
    public interface IContraMastersExcelExporter
    {
        FileDto ExportToFile(List<GetContraMasterForViewDto> contraMasters);
    }
}
