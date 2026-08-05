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

    public class ProductGroupsExcelExporter(
        ITimeZoneConverter timeZoneConverter,
        IAbpSession abpSession,
        ITempFileCacheManager tempFileCacheManager)
        : NpoiExcelExporterBase(tempFileCacheManager), IProductGroupsExcelExporter
    {
        private readonly IAbpSession _abpSession = abpSession;

        private readonly ITimeZoneConverter _timeZoneConverter = timeZoneConverter;

        public FileDto ExportToFile(List<GetProductGroupForViewDto> productGroups)
        {
            return CreateExcelPackage(
                "ProductGroups.xlsx",
                excelPackage =>
                {
                    var sheet = excelPackage.CreateSheet("ProductGroups");

                    AddHeader(
                        sheet,
                        "Name",
                        "IsDefult",
                        "GroupUnder"
                    );

                    AddObjects(
                        sheet, productGroups,
                        d => d.Name,
                        d => d.IsDefult,
                        d => d.GroupUnder
                    );
                });
        }
    }
}
