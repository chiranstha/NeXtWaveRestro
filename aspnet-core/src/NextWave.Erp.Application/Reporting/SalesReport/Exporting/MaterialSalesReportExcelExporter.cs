using NextWave.Erp.DataExporting.Excel.NPOI;
using NextWave.Erp.Dto;
using NextWave.Erp.Reporting.Dto;
using NextWave.Erp.Storage;
using System.Collections.Generic;

namespace NextWave.Erp.Reporting.SalesReport.Exporting
{

    public class MaterialSalesReportExcelExporter(ITempFileCacheManager tempFileCacheManager)
        : NpoiExcelExporterBase(tempFileCacheManager), IMaterialSalesReportExcelExporter
    {
        public FileDto ExportToFile(List<MaterialSalesReportDto> materialSales)
        {
            return CreateExcelPackage(
                "MaterialSalesReport.xlsx",
                excelPackage =>
                {
                    var sheet = excelPackage.CreateSheet("MaterialSalesReport");

                    AddHeader(
                        sheet,
                        "FiscalYear",
                        "CustomerName",
                        "CustomerPan",
                        "BillDate",
                        "BillNo",
                        "Payment Method",
                        "Gross Amount",
                        "Discount Amount",
                        "Taxable Amount",
                        "Tax Amount",
                        "Grand Total",
                        "Printed TIme",
                        "Entered By",
                        "Printed By",
                        "VAT Refund Amount",
                        "Transaction Id",
                        "Sync IRD",
                        "Bill Print",
                        "RealTime",
                        "Active"
                    );
                    AddObjects(
                        sheet, materialSales,
                        d => d.FiscalYear,
                        d => d.CustomerName,
                        d => d.CustomerPan,
                        d => d.BillDate,
                        d => d.BillNo,
                        d => d.PaymentMethod,
                        d => d.Amount,
                        d => d.Discount,
                        d => d.TaxableAmount,
                        d => d.TaxAmount,
                        d => d.TotalAmount,
                        d => d.PrintedTime,
                        d => d.EnteredBy,
                        d => d.PrintedBy,
                        d => d.VatRefundAmount,
                        d => d.TransactionId,
                        d => d.SyncIrd,
                        d => d.BillPrint,
                        d => d.RealTime,
                        d => d.Active
                    );
                });
        }
    }
}
