using Abp.Runtime.Session;
using Abp.Timing.Timezone;
using NextWave.Erp.DataExporting.Excel.NPOI;
using NextWave.Erp.Dto;
using NextWave.Erp.Purchase.Dtos;
using NextWave.Erp.Storage;
using NextWave.Erp.Transaction.Dtos;
using System.Collections.Generic;

namespace NextWave.Erp.Purchase.Exporting
{

    public class PurchaseMastersExcelExporter(
        ITimeZoneConverter timeZoneConverter,
        IAbpSession abpSession,
        ITempFileCacheManager tempFileCacheManager)
        : NpoiExcelExporterBase(tempFileCacheManager), IPurchaseMastersExcelExporter
    {
        public FileDto ExportToFile(List<GetPurchaseMasterForViewDto> purchaseMasters)
        {
            return CreateExcelPackage(
                "Purchase.xlsx",
                excelPackage =>
                {
                    var sheet = excelPackage.CreateSheet("Purchase");

                    AddHeader(
                        sheet,
                    //    "BranchName",
                        "VoucherNo",
                        "Date",
                        "DateMiti",
                        "AccountName",
                        "VendorInvoiceNo",
                        "VendorInvoiceDate",
                        "TotalAmount",
                        "BillDiscount",
                        "TotalTax",
                        "GrandTotal",
                        "Narration"
                    );

                    AddObjects(
                        sheet, purchaseMasters,
                        //   d => d.BranchName,
                        d => d.VoucherNo,
                        d => timeZoneConverter.Convert(d.Date, abpSession.TenantId,
                            abpSession.GetUserId()),
                        d => d.DateMiti,
                        d => d.LedgerName,
                        d => d.VendorInvoiceNo,
                        //d => timeZoneConverter.Convert(d.VendorInvoiceDate, abpSession.TenantId,
                        //    abpSession.GetUserId()),
                        d => d.TotalAmount,
                        d => d.BillDiscount,
                        d => d.TotalTax,
                        d => d.GrandTotal,
                        d => d.Narration
                    );

                    for (var i = 1; i <= purchaseMasters.Count; i++)
                        SetCellDataFormat(sheet.GetRow(i).Cells[2], "yyyy-mm-dd");
                    sheet.AutoSizeColumn(2);
                    for (var i = 1; i <= purchaseMasters.Count; i++)
                        SetCellDataFormat(sheet.GetRow(i).Cells[6], "yyyy-mm-dd");
                    sheet.AutoSizeColumn(4);
                });
        }

        public FileDto ExportToFileDetails(GetPurchaseMasterExcelExportMasterDto purchaseMasters)
        {
            return CreateExcelPackage(
                "Purchase.xlsx",
                excelPackage =>
                {
                    var sheet = excelPackage.CreateSheet("Purchase");
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
                        "TotalAmount",
                        "BillDiscount",
                        "TotalTax",
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
                        //d => timeZoneConverter.Convert(d.VendorInvoiceDate, abpSession.TenantId,
                        //    abpSession.GetUserId()),
                        d => d.TotalAmount,
                        d => d.BillDiscount,
                        d => d.TotalTax,
                        d => d.GrandTotal,
                        d => d.Narration,
                        d => d.Details
                    );

                    //for (var i = 1; i <= purchaseMasters.Count; i++)
                    //    SetCellDataFormat(sheet.GetRow(i).Cells[2], "yyyy-mm-dd");
                    //sheet.AutoSizeColumn(2);
                    //for (var i = 1; i <= purchaseMasters.Count; i++)
                    //    SetCellDataFormat(sheet.GetRow(i).Cells[6], "yyyy-mm-dd");
                    ColumnResize(sheet, 2);
                });
        }
    }
}
