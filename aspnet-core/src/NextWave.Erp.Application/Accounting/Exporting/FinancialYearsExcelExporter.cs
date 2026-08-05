using Abp.Runtime.Session;
using Abp.Timing.Timezone;
using NextWave.Erp.Accounting.Dtos;
using NextWave.Erp.DataExporting.Excel.NPOI;
using NextWave.Erp.Dto;
using NextWave.Erp.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Accounting.Exporting
{

    public class FinancialYearsExcelExporter(
        ITimeZoneConverter timeZoneConverter,
        IAbpSession abpSession,
        ITempFileCacheManager tempFileCacheManager)
        : NpoiExcelExporterBase(tempFileCacheManager), IFinancialYearsExcelExporter
    {
        public FileDto ExportToFile(List<GetFinancialYearForViewDto> financialYears)
        {
            return CreateExcelPackage(
                "FinancialYears.xlsx",
                excelPackage =>
                {
                    var sheet = excelPackage.CreateSheet("FinancialYears");

                    AddHeader(
                        sheet,
                        "FromDate",
                        "ToDate",
                        "FromMiti",
                        "ToMiti",
                        "Status"
                    );

                    AddObjects(
                        sheet, financialYears,
                        d => timeZoneConverter.Convert(d.FromDate, abpSession.TenantId, abpSession.GetUserId()),
                        d => timeZoneConverter.Convert(d.ToDate, abpSession.TenantId, abpSession.GetUserId()),
                        d => d.FromMiti,
                        d => d.ToMiti,
                        d => d.Status
                    );

                    for (var i = 1; i <= financialYears.Count; i++)
                        SetCellDataFormat(sheet.GetRow(i).Cells[1], "yyyy-mm-dd");
                    sheet.AutoSizeColumn(1);
                    for (var i = 1; i <= financialYears.Count; i++)
                        SetCellDataFormat(sheet.GetRow(i).Cells[2], "yyyy-mm-dd");
                    sheet.AutoSizeColumn(2);
                });
        }
    }
}
