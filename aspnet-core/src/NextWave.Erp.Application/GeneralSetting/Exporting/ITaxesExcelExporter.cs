using NextWave.Erp.Dto;
using NextWave.Erp.GeneralSetting.Dtos;
using System.Collections.Generic;

namespace NextWave.Erp.GeneralSetting.Exporting
{
    public interface ITaxesExcelExporter
    {
        FileDto ExportToFile(List<GetTaxForViewDto> taxes);
    }
}
