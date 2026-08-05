using Abp.Runtime.Session;
using Abp.Timing.Timezone;
using NextWave.Erp.DataExporting.Excel.NPOI;
using NextWave.Erp.Dto;
using NextWave.Erp.Inventory.Dtos;
using NextWave.Erp.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Inventory.Exporting
{

    public class UnitsExcelExporter(
        ITimeZoneConverter timeZoneConverter,
        IAbpSession abpSession,
        ITempFileCacheManager tempFileCacheManager)
        : NpoiExcelExporterBase(tempFileCacheManager), IUnitsExcelExporter
    {
        private readonly IAbpSession _abpSession = abpSession;

        private readonly ITimeZoneConverter _timeZoneConverter = timeZoneConverter;

        public FileDto ExportToFile(List<GetUnitForViewDto> units)
        {
            return CreateExcelPackage(
                "Units.xlsx",
                excelPackage =>
                {
                    var sheet = excelPackage.CreateSheet("Units");

                    AddHeader(
                        sheet,
                        "Name",
                        "FormalName"
                    );

                    AddObjects(
                        sheet, units,
                        d => d.Name,
                        d => d.FormalName
                    );
                });
        }
    }
}
