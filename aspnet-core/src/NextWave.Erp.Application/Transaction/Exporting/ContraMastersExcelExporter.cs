using Abp.Runtime.Session;
using Abp.Timing.Timezone;
using NextWave.Erp.DataExporting.Excel.NPOI;
using NextWave.Erp.Dto;
using NextWave.Erp.Storage;
using NextWave.Erp.Transaction.Dtos;
using System.Collections.Generic;

namespace NextWave.Erp.Transaction.Exporting
{
    public class ContraMastersExcelExporter(
    ITimeZoneConverter timeZoneConverter,
    IAbpSession abpSession,
    ITempFileCacheManager tempFileCacheManager)
    : NpoiExcelExporterBase(tempFileCacheManager), IContraMastersExcelExporter
    {
        private readonly IAbpSession _abpSession = abpSession;

        private readonly ITimeZoneConverter _timeZoneConverter = timeZoneConverter;

        public FileDto ExportToFile(List<GetContraMasterForViewDto> contraMasters)
        {
            return CreateExcelPackage(
                "ContraMasters.xlsx",
                excelPackage =>
                {
                    var sheet = excelPackage.CreateSheet("ContraMasters");

                    AddHeader(
                        sheet,
                        "Type",
                        "Date",
                        "LedgerName",
                        "Total Amount"
                    );

                    AddObjects(
                        sheet, contraMasters,
                        d => d.Type,
                        d => d.DateMiti,
                        d => d.LedgerName,
                        d => d.TotalAmount
                    );
                });
        }
    }
}