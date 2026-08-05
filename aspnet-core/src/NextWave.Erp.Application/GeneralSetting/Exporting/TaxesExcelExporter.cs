using Abp.Runtime.Session;
using Abp.Timing.Timezone;
using NextWave.Erp.DataExporting.Excel.NPOI;
using NextWave.Erp.Dto;
using NextWave.Erp.GeneralSetting.Dtos;
using NextWave.Erp.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.GeneralSetting.Exporting
{

    public class TaxesExcelExporter(
        ITimeZoneConverter timeZoneConverter,
        IAbpSession abpSession,
        ITempFileCacheManager tempFileCacheManager)
        : NpoiExcelExporterBase(tempFileCacheManager), ITaxesExcelExporter
    {
        private readonly IAbpSession _abpSession = abpSession;

        private readonly ITimeZoneConverter _timeZoneConverter = timeZoneConverter;

        public FileDto ExportToFile(List<GetTaxForViewDto> taxes)
        {
            return CreateExcelPackage(
                "Taxes.xlsx",
                excelPackage =>
                {
                    var sheet = excelPackage.CreateSheet("Taxes");

                    AddHeader(
                        sheet,
                        "Name",
                        "IsActive",
                        "Rate"
                    );

                    AddObjects(
                        sheet, taxes,
                        d => d.Name,
                        d => d.IsActive,
                        d => d.Rate
                    );
                });
        }
    }
}
