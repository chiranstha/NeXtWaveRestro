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

    public class AccountGroupsExcelExporter(
        ITimeZoneConverter timeZoneConverter,
        IAbpSession abpSession,
        ITempFileCacheManager tempFileCacheManager)
        : NpoiExcelExporterBase(tempFileCacheManager), IAccountGroupsExcelExporter
    {
        private readonly IAbpSession _abpSession = abpSession;

        private readonly ITimeZoneConverter _timeZoneConverter = timeZoneConverter;

        public FileDto ExportToFile(List<GetAccountGroupForViewDto> accountGroups)
        {
            return CreateExcelPackage(
                "AccountGroups.xlsx",
                excelPackage =>
                {
                    var sheet = excelPackage.CreateSheet("AccountGroups");
                    AddHeader(
                        sheet,
                        "Name",
                        "Narration",
                        "IsDefault",
                        "AffectGrossProfit",
                        "Nature",
                        "GroupUnder"
                    );

                    AddObjects(
                        sheet, accountGroups,
                        d => d.Name,
                        d => d.Narration,
                        d => d.IsDefault,
                        d => d.AffectGrossProfit,
                        d => d.Nature,
                        d => d.AccountGroupName
                    );
                });
        }
    }
}
