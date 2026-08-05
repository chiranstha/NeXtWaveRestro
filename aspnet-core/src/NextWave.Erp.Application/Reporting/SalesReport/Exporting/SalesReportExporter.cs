using Abp.Runtime.Session;
using Abp.Timing.Timezone;
using NextWave.Erp.DataExporting.Excel.NPOI;
using NextWave.Erp.Dto;
using NextWave.Erp.Reporting.Dto;
using NextWave.Erp.Sales.Dtos;
using NextWave.Erp.Storage;
using System.Collections.Generic;

namespace NextWave.Erp.Reporting.SalesReport.Exporting
{
    public class SalesReportExporter(
        ITimeZoneConverter timeZoneConverter,
        IAbpSession abpSession,
        ITempFileCacheManager tempFileCacheManager)
        : NpoiExcelExporterBase(tempFileCacheManager), ISalesReportExport
    {
        public FileDto ExportToFile(List<PurchaseReportList> products)
        {
            {
                return CreateExcelPackage(
                    "Sales.xlsx",
                    excelPackage =>
                    {
                        var sheet = excelPackage.CreateSheet("Sales");
                        AddHeader(
                            sheet,
                            "Date",
                            "Ledger",
                            "Totalamount"
                        );

                        AddObjects(
                            sheet, products,
                            l => l.Date,
                            l => l.LedgerName,
                            l => l.TotalAmount
                        );
                    });
            }
        }

        public FileDto NewExportToFile(List<SalesReportDtoNew> sales)
        {
            return CreateExcelPackage(
                "Sales.xlsx",
                excelPackage =>
                {
                    var sheet = excelPackage.CreateSheet("Sales");
                    AddHeader(
                        sheet,
                        "Datemiti",
                        "Voucherno",
                        "Returnvoucherno",
                        "Partyname",
                        "Paymenymethod",
                        "PAN",
                        "Netamount",
                        "Taxableamount",
                        "Taxamount",
                        "Discount",
                        "Grandtotal"
                    );

                    AddObjects(
                        sheet, sales,
                        x => x.DateMiti,
                        x => x.VoucherNo,
                        x => x.ReturnVoucherNo,
                        x => x.PartyName,
                        x => x.PaymentMethod,
                        x => x.Pan,
                        x => x.TotalAmount,
                        x => x.TaxableAmount,
                        x => x.TaxAmount,
                        x => x.Discount,
                        x => x.GrandTotal
                    );
                });
        }

        public FileDto ExportToFileDetails(GetSalesMasterExportMasterDto data)
        {
            return CreateExcelPackage(
                "Sales.xlsx",
                excelPackage =>
                {
                    var sheet = excelPackage.CreateSheet("Sales");
                    AddMasterHeader(sheet, 0, 0, 1, 6, data.Company);
                    AddMasterHeader(sheet, 1, 1, 1, 6, "Phone No. : " + data.Phone);
                    AddMasterHeader(sheet, 2, 2, 1, 6, "Address : " + data.Address);

                    AddHeaderWithMainHeader(
                        sheet, 3,
                        "मिती",
                        "बिजक नम्बर",
                        "खरिदकर्ताको नाम",
                        "खरिदकर्ताको स्थायी लेखा नम्बर",
                        "वस्तु वा सेवाको नाम",
                        "वस्तु वा सेवाको परिमाण",
                        "कुल रकम",
                        "छुट रकम",
                        "शुद्ध रकम",
                        "कर योग्य रकम",
                        "कर रकम",
                        "जम्मा बिक्री"
                    //"स्थानिय कर छुटको बिक्री मुल्य(रु)",
                    //"मुल्य(रु)",
                    //"कर(रु)"
                    );

                    AddObjectsWithDetails(
                        sheet, 4, data.Details,
                        d => d.DateMiti,
                        d => d.VoucherNo,
                        d => d.LedgerName,
                        d => d.PanNo,
                        d => d.ProductName,
                        d => d.Quantity,
                        d => d.GrossAmount,
                        d => d.BillDiscount,
                        d => d.NetAmount,
                        d => d.TaxableAmount,
                        d => d.TaxAmount,
                        d => d.TotalAmount,
                        //      _ => _.AdditionalCost,
                        //     _ => _.TotalAmount,
                        d => d.Details
                    );
                });
        }
    }
}
