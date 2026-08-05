using Abp.Runtime.Session;
using Abp.Timing.Timezone;
using NextWave.Erp.DataExporting.Excel.NPOI;
using NextWave.Erp.Dto;
using NextWave.Erp.Sales.Dtos;
using NextWave.Erp.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Sales.Exporting
{

    public class SalesMastersNewExcelExporter(
        ITimeZoneConverter timeZoneConverter,
        IAbpSession abpSession,
        ITempFileCacheManager tempFileCacheManager)
        : NpoiExcelExporterBase(tempFileCacheManager), ISalesMastersNewExcelExporter
    {
        private readonly IAbpSession _abpSession = abpSession;

        private readonly ITimeZoneConverter _timeZoneConverter = timeZoneConverter;

        public FileDto ExportToFile(List<GetSalesMasterExportDto> salesMasters)
        {
            return CreateExcelPackage(
                "Sales.xlsx",
                excelPackage =>
                {
                    var sheet = excelPackage.CreateSheet("Sales");

                    AddHeader(
                        sheet,
                        "मिती",
                        "बिजक नम्बर",
                        "खरिदकर्ताको नाम",
                        "खरिदकर्ताको स्थायी लेखा नम्बर",
                        "वस्तु वा सेवाको नाम",
                        "वस्तु वा सेवाको परिमाण",
                        "जम्मा बिक्री",
                        "स्थानिय कर छुटको बिक्री मुल्य(रु)",
                        "मुल्य(रु)",
                        "कर(रु)"
                    );

                    AddObjects(
                        sheet, salesMasters,
                        d => d.DateMiti,
                        d => d.VoucherNo,
                        d => d.LedgerName,
                        d => d.PanNo,
                        d => d.ProductName,
                        d => d.Quantity,
                        d => d.TotalAmount,
                        d => d.BillDiscount,
                        d => d.TaxableAmount,
                        d => d.TaxAmount
                    );
                    AddHeaderSecoundWithMerge(sheet, 5, 2, 5, " ");
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
                        "जम्मा बिक्री",
                        "स्थानिय कर छुटको बिक्री मुल्य(रु)",
                        "मुल्य(रु)",
                        "कर(रु)"
                    );

                    AddObjectsWithDetails(
                        sheet, 4, data.Details,
                        d => d.DateMiti,
                        d => d.VoucherNo,
                        d => d.LedgerName,
                        d => d.PanNo,
                        d => d.ProductName,
                        d => d.Quantity,
                        d => d.TotalAmount,
                        d => d.BillDiscount,
                        d => d.TaxableAmount,
                        d => d.TaxAmount,
                        d => d.TotalAmount,
                        d => d.Details
                    );
                });
        }

        //public FileDto ExportToFileSwastik(SalesDtoForSwastik data)
        //{
        //    return CreateExcelPackage(
        //        "SalesForSwastik.xlsx",
        //        excelPackage =>
        //        {
        //            var sheet = excelPackage.CreateSheet("SalesReportForSwastik");
        //            AddHeaderSwastik(sheet, 10,
        //                "Bill Miti",
        //                "Bill No",
        //                "Party Name",
        //                "PAN No",
        //                "Goods/Service Name",
        //                "Goods/Service Qty",
        //                "Total Sales/Export",
        //                "Tax Exempted",
        //                "Exported Goods/Service Value",
        //                "Taxable Value",
        //                "Vat",
        //                "Country Of Export",
        //                "Exported PP NO",
        //                "Exported PP Miti"
        //            );

        //            AddObjectsSwastik(
        //                sheet, 11, data.Details,
        //                s => s.BillMiti,
        //                s => s.BillNo,
        //                s => s.PartyName,
        //                s => s.Pan,
        //                s => s.Goods,
        //                s => s.Qty,
        //                s => s.TotalSales,
        //                s => s.TaxExempted,
        //                s => s.ExportedValue,
        //                s => s.TaxableValue,
        //                s => s.Vat,
        //                s => s.CountryOfExport,
        //                s => s.ExportedPpno,
        //                s => s.ExportedPpMiti
        //            );
        //        });
        //}

        public FileDto ExportToFileWithDetails(List<GetSalesMasterExportWithProductDto> salesMasters)
        {
            return CreateExcelPackage(
                "Sales.xlsx",
                excelPackage =>
                {
                    var sheet = excelPackage.CreateSheet("Sales");

                    AddHeader(
                        sheet,
                        "मिती",
                        "बिजक नम्बर",
                        "खरिदकर्ताको नाम",
                        "खरिदकर्ताको स्थायी लेखा नम्बर",
                        "वस्तु वा सेवाको नाम",
                        "वस्तु वा सेवाको परिमाण",
                        "जम्मा बिक्री",
                        "स्थानिय कर छुटको बिक्री मुल्य(रु)",
                        "मुल्य(रु)",
                        "कर(रु)"
                    );

                    AddObjects(
                        sheet, salesMasters,
                        d => d.DateMiti,
                        d => d.VoucherNo,
                        d => d.LedgerName,
                        d => d.PanNo,
                        d => d.ProductName,
                        d => d.Quantity,
                        d => d.GrandTotal,
                        d => d.BillDiscount,
                        d => d.TaxableAmount,
                        d => d.TaxAmount
                    );
                    AddHeaderSecoundWithMerge(sheet, 5, 2, 5, " ");
                });
        }
    }
}
