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

    public class ProductsExcelExporter(
        ITimeZoneConverter timeZoneConverter,
        IAbpSession abpSession,
        ITempFileCacheManager tempFileCacheManager)
        : NpoiExcelExporterBase(tempFileCacheManager), IProductsExcelExporter
    {
        private readonly IAbpSession _abpSession = abpSession;

        private readonly ITimeZoneConverter _timeZoneConverter = timeZoneConverter;

        public FileDto ExportToFile(List<GetProductForViewDto> products)
        {
            return CreateExcelPackage(
                "Products.xlsx",
                excelPackage =>
                {
                    var sheet = excelPackage.CreateSheet("Products");

                    AddHeader(
                        sheet,
                        "Name",
                        "Mrp",
                        "SalesRate",
                        "PurchaseRate",
                        "MinimumStock",
                        "IsAllowBatch",
                        "IsMultipleUnit",
                        "IsBom",
                        "IsOpeningStock",
                        "IsShowRemember",
                        "Description",
                        "IsActive",
                        "GuaranteePeriod",
                        "WarrantyPeriod",
                        "ModelNo",
                        "ProductGroup",
                        "UnitName"
                    );

                    AddObjects(
                        sheet, products,
                        d => d.Name,
                        d => d.Mrp,
                        d => d.SalesRate,
                        d => d.PurchaseRate,
                        d => d.MinimumStock,
                        d => d.IsOpeningStock,
                        d => d.Description,
                        d => d.IsActive,
                        d => d.ProductGroupName,
                        d => d.UnitName
                    );
                });
        }
    }
}
