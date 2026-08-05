using NextWave.Erp.DataExporting.Excel.NPOI;
using NextWave.Erp.Dto;
using NextWave.Erp.Inventory.Dtos;
using NextWave.Erp.Storage;
using System.Collections.Generic;

namespace NextWave.Erp.Inventory.Exporting
{

    public class ProductNewExcelExporter(ITempFileCacheManager tempFileCacheManager)
        : NpoiExcelExporterBase(tempFileCacheManager), IProductNewExcelExporter
    {
        public FileDto ExportHsCode(List<HsCodeDto> data)
        {
            return CreateExcelPackage(
                "HSCodeExcel.xlsx",
                excelPackage =>
                {
                    var sheet = excelPackage.CreateSheet("HSCodeExcel");

                    AddHeader(
                        sheet,
                        "ProductId",
                        "ProductName",
                        "HSCode"
                    );

                    AddObjects(
                        sheet, data,
                        d => d.ProductId.ToString(),
                        d => d.ProductName,
                        d => d.HsCode
                    );
                });
        }

        public FileDto ExportToFile(List<ImportProductDto> products)
        {
            {
                return CreateExcelPackage(
                    "Products.xlsx",
                    excelPackage =>
                    {
                        var sheet = excelPackage.CreateSheet("Products");

                        AddHeader(
                            sheet,
                            "ProductCode",
                            "Name",
                            "ProductGroup",
                            "UnitName",
                            "PurchaseRate",
                            "SalesRate",
                            "MRP",
                            "OpeningQty",                            
                            "HsCode",
                            "Taxable"
                        );

                        AddObjects(
                            sheet, products,
                            d => d.ProductCode,
                            d => d.Name,
                            d => d.ProductGroupName,
                            d => d.UnitName,
                            d => d.PurchaseRate,
                            d => d.SalesRate,
                            d => d.Mrp,
                            d => d.OpeningQty,
                            d => d.HsCode,
                            d => d.IsTaxable
                        );
                    });
            }
        }
    }
}
