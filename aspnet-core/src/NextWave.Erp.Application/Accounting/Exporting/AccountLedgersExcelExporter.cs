using Abp.Runtime.Session;
using Abp.Timing.Timezone;
using NextWave.Erp.Accounting.Dtos;
using NextWave.Erp.DataExporting.Excel.NPOI;
using NextWave.Erp.Dto;
using NextWave.Erp.Storage;
using System.Collections.Generic;

namespace NextWave.Erp.Accounting.Exporting
{

    public class AccountLedgersExcelExporter(
        ITimeZoneConverter timeZoneConverter,
        IAbpSession abpSession,
        ITempFileCacheManager tempFileCacheManager)
        : NpoiExcelExporterBase(tempFileCacheManager), IAccountLedgersExcelExporter
    {
        private readonly IAbpSession _abpSession = abpSession;

        private readonly ITimeZoneConverter _timeZoneConverter = timeZoneConverter;

        public FileDto ExportToFile(List<GetAccountLedgerForExportDto> accountLedgers)
        {
            return CreateExcelPackage(
                "AccountLedgers.xlsx",
                excelPackage =>
                {
                    var sheet = excelPackage.CreateSheet("AccountLedgers");
                    AddHeader(
                        sheet,
                        "Name",
                        "Phone",
                        "OpeningBalance",
                        "CrOrDr",
                        "CreditLimit",
                        "AccountGroupName",
                        "PAN",
                        "Address"
                    );

                    AddObjects(
                        sheet, accountLedgers,
                        d => d.Name,
                        d => d.Phone,
                        d => d.OpeningBalance,
                        d => d.CrOrDr,
                        d => d.CreditLimit,
                        d => d.AccountGroupName,
                        d => d.Pan,
                        d => d.Address
                    );
                });
        }
    }
}
