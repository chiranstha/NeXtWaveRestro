using NextWave.Erp.DataExporting.Excel.NPOI;
using NextWave.Erp.Dto;
using NextWave.Erp.Reporting.Dto;
using NextWave.Erp.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Reporting.TaxReport.Exporting
{

    public class SalesTaxExcelExporter(ITempFileCacheManager tempFileCacheManager)
        : NpoiExcelExporterBase(tempFileCacheManager), ISalesTaxExcelExporter
    {
        public FileDto ExportToFile(List<TaxReportDto> salesTax)
        {
            return CreateExcelPackage(
                "SalesTaxReport.xlsx",
                excelPackage =>
                {
                    var sheet = excelPackage.CreateSheet("SalesTaxReport");
                    AddHeader(
                        sheet,
                        "DateMiti",
                        "VoucherNo",
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
                        "LocalTaxAmount",
                        "ImportTaxableAmount",
                        "ImportTaxAmount",
                        "TaxableCapitalizeAmount",
                        "TaxableCapitalizeTax"
                    );
                    AddObjects(
                        sheet, salesTax,
                        d => d.DateMiti,
                        d => d.VoucherNo,
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

        public FileDto ExportToFileDetail(List<Above1LakhMasterDto> salesTax)
        {
            return CreateExcelPackage(
                "SalesTaxReport.xlsx",
                excelPackage =>
                {
                    var sheet = excelPackage.CreateSheet("SalesTaxReport");
                    AddHeader(
                        sheet,
                        "DateMiti",
                        "VoucherNo",
                        "LedgerName",
                        "Address",
                        "PAN",
                        "GrossAmount",
                        "Discount",
                        "NetAmount",
                        "TaxableAmount",
                        "NonTaxableAmount",
                        "TaxAmount",
                        "GrandTotal",
                        "ProductName"
                    );
                    AddObjects(
                        sheet, salesTax,
                        d => d.Date,
                        d => d.BillNo,
                        d => d.PartyName,
                        d => d.Address,
                        d => d.Pan,
                        d => d.GrossAmount,
                        d => d.Discount,
                        d => d.NetAmount,
                        d => d.TaxableAmount,
                        d => d.NonTaxableAmount,
                        d => d.TaxAmount,
                        d => d.GrandTotal,
                        d => d.ProductName
                    );
                });
        }
    }
}
