using NextWave.Erp.DataExporting.Excel.NPOI;
using NextWave.Erp.Dto;
using NextWave.Erp.Reporting.Dto;
using NextWave.Erp.Storage;
using System.Collections.Generic;
using System.Linq;

namespace NextWave.Erp.Reporting.TaxReport.Exporting
{
    public class PurchaseTaxExcelExporter(ITempFileCacheManager tempFileCacheManager)
        : NpoiExcelExporterBase(tempFileCacheManager), IPurchaseTaxExcelExporter
    {
        public FileDto ExportToFile(List<TaxReportDto> salesTax)
        {
            return CreateExcelPackage(
                "PurchaseTaxReport.xlsx",
                excelPackage =>
                {
                    var sheet = excelPackage.CreateSheet("PurchaseTaxReport");

                    AddHeader(
                        sheet,
                        "DateMiti",
                        "VoucherNo",
                        "Vendor Invoice No",
                        "LedgerName",
                        "PAN",
                        "ProductCategories",
                        "ProductNames",
                        "Qty",
                        "UnitNames",
                        "GrossAmount",
                        "Discount",
                        "NetAmount",
                        "NonVatableAmount",
                        "TotalAmount",
                        "LocalTaxableAmount",
                        "LocalTaxAmount"
                    );
                    AddObjects(
                        sheet, salesTax,
                        d => d.DateMiti,
                        d => d.VoucherNo,
                        d => d.VendorInvoiceNo,
                        d => d.LedgerName,
                        d => d.Pan,
                        d => d.ProductCategoryName,
                        d => d.ProductName,
                        d => d.Quantity,
                        d => d.UnitsName,
                        d => d.GrossAmount,
                        d => d.Discount,
                        d => d.NetAmount,
                        d => d.NonTaxableAmount,
                        d => d.Amount,
                        d => d.LocalTaxableAmount,
                        d => d.LocalTaxAmount
                    );
                    AddTailer(sheet, 8, salesTax.Count,
                        salesTax.Sum(x => x.GrossAmount),
                        salesTax.Sum(x => x.Discount),
                        salesTax.Sum(x => x.NetAmount),
                        salesTax.Sum(x => x.NonTaxableAmount),
                        salesTax.Sum(x => x.Amount),
                        salesTax.Sum(x => x.LocalTaxableAmount),
                        salesTax.Sum(x => x.LocalTaxAmount)
                    );
                });
        }
    }
}
