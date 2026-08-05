using Abp.Runtime.Session;
using Abp.Timing.Timezone;
using NextWave.Erp.DataExporting.Excel.NPOI;
using NextWave.Erp.Dto;
using NextWave.Erp.Sales.Dtos;
using NextWave.Erp.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Sales.Exporting
{
    public class SalesReturnMastersExcelExporter(
      ITimeZoneConverter timeZoneConverter,
      IAbpSession abpSession,
      ITempFileCacheManager tempFileCacheManager)
      : NpoiExcelExporterBase(tempFileCacheManager), ISalesReturnMastersExcelExporter
    {
        private readonly IAbpSession _abpSession = abpSession;

        private readonly ITimeZoneConverter _timeZoneConverter = timeZoneConverter;

        public FileDto ExportToFile(List<GetSalesReturnMasterForViewDto> salesReturnMasters)
        {
            return CreateExcelPackage(
                "SalesReturnMasters.xlsx",
                excelPackage =>
                {
                    var sheet = excelPackage.CreateSheet("SalesReturnMasters");

                    AddHeader(
                        sheet,
                        "Date",
                        "VoucherNo",
                        "LedgerName",
                        "TotalAmount",
                        "TaxAmount",
                        "GrandTotal",
                        "Description"
                    );

                    AddObjects(
                        sheet, salesReturnMasters,
                        d => d.DateMiti,
                        d => d.VoucherNo,
                        d => d.LedgerName,
                        d => d.TotalAmount,
                        d => d.TaxAmount,
                        d => d.GrandTotal,
                        d => d.Description
                    );
                });
        }
    }
}
