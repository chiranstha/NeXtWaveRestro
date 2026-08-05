using Abp.Runtime.Session;
using Abp.Timing.Timezone;
using NextWave.Erp.DataExporting.Excel.NPOI;
using NextWave.Erp.Dto;
using NextWave.Erp.Purchase.Dtos;
using NextWave.Erp.Reporting.Dto;
using NextWave.Erp.Storage;
using System.Collections.Generic;

namespace NextWave.Erp.Reporting.PurchaseReport.Exporting
{

    public class PurchaseReportExport(
        ITimeZoneConverter timeZoneConverter,
        IAbpSession abpSession,
        ITempFileCacheManager tempFileCacheManager)
        : NpoiExcelExporterBase(tempFileCacheManager), IPurchaseReportExport
    {
        public FileDto ExportToFile(List<PurchaseReportList> products)
        {
            {
                return CreateExcelPackage(
                    "PurchaseReport.xlsx",
                    excelPackage =>
                    {
                        var sheet = excelPackage.CreateSheet("PurchaseReport");

                        AddHeader(
                            sheet,
                            "Invoice",
                            "Date",
                            "LedgerName",
                            "TotalTaxAmount",
                            "Totalamount",
                            "Grandtotal"
                        );

                        AddObjects(
                            sheet, products,
                            l => l.InvoiceNo,
                            l => l.Date,
                            l => l.LedgerName,
                            l => l.TotalTaxAmount,
                            l => l.TotalAmount,
                            l => l.GrandTotal
                        );
                    });
            }
        }

        public FileDto ExportToFileDetails(GetPurchaseMasterExcelExportMasterDto purchaseMasters)
        {
            return CreateExcelPackage(
                "Purchase.xlsx",
                excelPackage =>
                {
                    var sheet = excelPackage.CreateSheet(L("Purchase"));
                    AddMasterHeader(sheet, 0, 0, 1, 6, purchaseMasters.Branch);
                    AddMasterHeader(sheet, 1, 1, 1, 6, "Phone No. : " + purchaseMasters.PhoneNo);
                    AddMasterHeader(sheet, 2, 2, 1, 6, "Address : " + purchaseMasters.Address);

                    AddHeaderWithMainHeader(
                        sheet, 3,
                        "BranchName",
                        "VoucherNo",
                        "Date",
                        "DateMiti",
                        "AccountName",
                        "VendorInvoiceNo",
                        "TotalTax",
                        "TotalAmount",
                        "BillDiscount",
                        "GrandTotal",
                        "Narration"
                    );

                    AddObjectsWithDetails(
                        sheet, 4, purchaseMasters.Details,
                        d => d.BranchName,
                        d => d.VoucherNo,
                        d => timeZoneConverter.Convert(d.Date, abpSession.TenantId,
                            abpSession.GetUserId()),
                        d => d.DateMiti,
                        d => d.LedgerName,
                        d => d.VendorInvoiceNo,

                        d => d.TotalTax,
                        d => d.TotalAmount,
                        d => d.BillDiscount,
                        d => d.GrandTotal,
                        d => d.Narration,
                        d => d.Details
                    );
                    ColumnResize(sheet, 10);
                });
        }

        public FileDto ExportToFileNepali(GetPurchaseMasterExcelExportMasterDto purchaseMasters)
        {
            return CreateExcelPackage(
                "Purchase Book.xlsx",
                excelPackage =>
                {
                    var sheet = excelPackage.CreateSheet(L("Purchase"));
                    AddMasterHeaderNepali(sheet, 0, 1, 18, 21, "पाना संख्या : ….........\t\t\t\r\n\t\t\t\r\n", true);
                    AddMasterHeaderNepali(sheet, 0, 1, 8, 17, $"करदाता दर्ता नम्बर : {purchaseMasters.Pan}", false);
                    AddMasterHeaderNepali(sheet, 0, 1, 4, 7, "खरिद खाता\t\t\t\r\n\t\t\t\r\n", false);
                    AddMasterHeaderNepali(sheet, 0, 0, 0, 3, $"करदाताको नाम : {purchaseMasters.Branch}", false);
                    AddMasterHeaderNepali(sheet, 1, 1, 0, 3,
                        $"कर अवघि : {purchaseMasters.FromDate} to {purchaseMasters.ToDate}", true);
                    AddMasterHeaderNepali(sheet, 2, 2, 0, 4, "बिजक/प्रज्ञान पत्र नं.\t\t\t\t\r\n", true);
                    AddMasterHeader(sheet, 3, 0, "मिती\r\n\r\n");
                    AddMasterHeader(sheet, 3, 1, "बिजक नंम्बर\r\n\r\n");
                    AddMasterHeader(sheet, 3, 2, "प्रज्ञापन पत्र नम्बर\r\n\r\n");
                    AddMasterHeader(sheet, 3, 3, "आपूर्तिकर्ताको नाम\r\n\r\n");
                    AddMasterHeader(sheet, 3, 4, "आपूर्तिकर्ताको स्थयी लेखा नंम्बर\r\n\r\n");
                    AddMasterHeaderNepali(sheet, 2, 3, 5, 5,
                        "खरीद/पैठारी गरीएका बस्तु वा सेवा मापन गर्ने इकार्इ\r\n\r\n\r\n", false);
                    AddMasterHeaderNepali(sheet, 2, 2, 6, 7, "जम्मा खरिद\t\r\n\t\r\n", false);
                    AddMasterHeaderNepali(sheet, 2, 2, 8, 9, "कर छुट हुने वस्तु वा सेवाको खरिद / पैठारी\t\r\n\t\r\n",
                        false);
                    AddMasterHeaderNepali(sheet, 2, 2, 10, 13, "कर योग्य खरिद (पूँजीगत बाहेक)\t\t\t\r\n\t\t\t\r\n", false);
                    AddMasterHeaderNepali(sheet, 2, 2, 14, 17, "कर योग्य पैठारी (पूँजीगत बाहेक) \t\t\t\r\n\t\t\t\r\n",
                        false);
                    AddMasterHeaderNepali(sheet, 2, 2, 18, 21, "पूँजीगत कर योग्य खरिद/ पैठारी कर\t\t\t\r\n\t\t\t\r\n",
                        false);
                    AddMasterHeader(sheet, 3, 6, "मूल्य (रू.)\r\n");
                    AddMasterHeader(sheet, 3, 7, "पै.\r\n");
                    AddMasterHeader(sheet, 3, 8, "मूल्य (रू.)\r\n");
                    AddMasterHeader(sheet, 3, 9, "पै.\r\n");
                    AddMasterHeader(sheet, 3, 10, "मूल्य (रू.)\r\n");
                    AddMasterHeader(sheet, 3, 11, "पै.\r\n");
                    AddMasterHeader(sheet, 3, 12, "कर (रू.)\r\n");
                    AddMasterHeader(sheet, 3, 13, "पै.\r\n");
                    AddMasterHeader(sheet, 3, 14, "मूल्य (रू.)\r\n");
                    AddMasterHeader(sheet, 3, 15, "पै.\r\n");
                    AddMasterHeader(sheet, 3, 16, "कर (रू.)\r\n");
                    AddMasterHeader(sheet, 3, 17, "पै.\r\n");
                    AddMasterHeader(sheet, 3, 18, "मूल्य (रू.)\r\n");
                    AddMasterHeader(sheet, 3, 19, "पै.\r\n");
                    AddMasterHeader(sheet, 3, 20, "कर (रू.)\r\n");
                    AddMasterHeader(sheet, 3, 21, "पै.\r\n");

                    AddObjectsNepali(
                        sheet, 4, purchaseMasters.Details,
                        d => d.DateMiti,
                        d => d.VoucherNo,
                        d => d.VendorInvoiceNo,
                        d => d.LedgerName,
                        d => d.LedgerPan,
                        _ => 0,
                        d => (int)d.GrandTotal,
                        d => d.GrandTotal - (int)d.GrandTotal,
                        d => (int)d.NonTaxable,
                        d => d.NonTaxable - (int)d.NonTaxable,
                        d => (int)d.TotalTaxableAmount,
                        d => d.TotalTaxableAmount - (int)d.TotalTaxableAmount,
                        d => (int)d.TotalTax,
                        d => d.TotalTax - (int)d.TotalTax
                    );
                });
        }
    }
}
