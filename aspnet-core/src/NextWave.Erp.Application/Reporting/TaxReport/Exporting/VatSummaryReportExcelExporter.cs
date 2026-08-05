using NextWave.Erp.DataExporting.Excel.NPOI;
using NextWave.Erp.Dto;
using NextWave.Erp.Reporting.Dto;
using NextWave.Erp.Storage;
using System.Collections.Generic;

namespace NextWave.Erp.Reporting.TaxReport.Exporting
{
    internal class VatSummaryReportExcelExporter(ITempFileCacheManager tempFileCacheManager)
    : NpoiExcelExporterBase(tempFileCacheManager), IVatSummaryReportExcelExporter
    {
        public FileDto ExportToFile(List<FinalVatSummaryReportDto> vatSummary)
        {
            return CreateExcelPackage(
                "VatSummaryReport.xlsx",
                excelPackage =>
                {
                    var sheet = excelPackage.CreateSheet("VatSummaryReport");
                    AddHeader(
                        sheet,
                        "TaxableSales",
                        "SalesVAT",
                        "ExemptSales",
                        "ZeroVatSales",
                        "TotalSales",
                        "CreditNoteTotal",
                        "CreditNoteVAT",
                        "NetSales",
                        "NetVatCollectionOnSales",
                        "TaxablePurchase",
                        "purchaseVAT",
                        "CapitalPurchase",
                        "CapitalPurchaseVAT",
                        "ExemptPurchase",
                        "TotalPurchase",
                        "DebitNoteTotal",
                        "DebitNoteVAT",
                        "NetPurchase",
                        "NetVatPaidOnPurchase",
                        "NetVATForTheMonth"
                    //header.Baisakh,
                    //header.Jestha,
                    //header.Asar,
                    //header.Shrawan,
                    //header.Bhadra,
                    //header.Asoj,
                    //header.Kartik,
                    //header.Mangsir,
                    //header.Poush,
                    //header.Magh,
                    //header.Falgun,
                    //header.Chaitra,
                    //header.Total
                    );
                    AddObjectsSwastik(
                        sheet, 2, vatSummary,
                        d => d.TaxableSales.Baisakh,
                        d => d.SalesVat.Baisakh,
                        d => d.ExemptSales.Baisakh,
                        d => d.ZeroVatSales.Baisakh,
                        d => d.TotalSales.Baisakh,
                        d => d.CreditNoteTotal.Baisakh,
                        d => d.CreditNoteVat.Baisakh,
                        d => d.NetSales.Baisakh,
                        d => d.NetVatCollectionOnSales.Baisakh,
                        d => d.TaxablePurchase.Baisakh,
                        d => d.PurchaseVat.Baisakh,
                        d => d.CapitalPurchase.Baisakh,
                        d => d.CapitalPurchaseVat.Baisakh,
                        d => d.ExemptPurchase.Baisakh,
                        d => d.TotalPurchase.Baisakh,
                        d => d.DebitNoteTotal.Baisakh,
                        d => d.DebitNoteVat.Baisakh,
                        d => d.NetPurchase.Baisakh,
                        d => d.NetVatPaidOnPurchase.Baisakh,
                        d => d.NetVatForTheMonth.Baisakh
                    );
                    AddObjectsSwastik(
                        sheet, 3, vatSummary,
                        d => d.TaxableSales.Jestha,
                        d => d.SalesVat.Jestha,
                        d => d.ExemptSales.Jestha,
                        d => d.ZeroVatSales.Jestha,
                        d => d.TotalSales.Jestha,
                        d => d.CreditNoteTotal.Jestha,
                        d => d.CreditNoteVat.Jestha,
                        d => d.NetSales.Jestha,
                        d => d.NetVatCollectionOnSales.Jestha,
                        d => d.TaxablePurchase.Jestha,
                        d => d.PurchaseVat.Jestha,
                        d => d.CapitalPurchase.Jestha,
                        d => d.CapitalPurchaseVat.Jestha,
                        d => d.ExemptPurchase.Jestha,
                        d => d.TotalPurchase.Jestha,
                        d => d.DebitNoteTotal.Jestha,
                        d => d.DebitNoteVat.Jestha,
                        d => d.NetPurchase.Jestha,
                        d => d.NetVatPaidOnPurchase.Jestha,
                        d => d.NetVatForTheMonth.Jestha
                    );
                    AddObjectsSwastik(
                        sheet, 4, vatSummary,
                        d => d.TaxableSales.Asar,
                        d => d.SalesVat.Asar,
                        d => d.ExemptSales.Asar,
                        d => d.ZeroVatSales.Asar,
                        d => d.TotalSales.Asar,
                        d => d.CreditNoteTotal.Asar,
                        d => d.CreditNoteVat.Asar,
                        d => d.NetSales.Asar,
                        d => d.NetVatCollectionOnSales.Asar,
                        d => d.TaxablePurchase.Asar,
                        d => d.PurchaseVat.Asar,
                        d => d.CapitalPurchase.Asar,
                        d => d.CapitalPurchaseVat.Asar,
                        d => d.ExemptPurchase.Asar,
                        d => d.TotalPurchase.Asar,
                        d => d.DebitNoteTotal.Asar,
                        d => d.DebitNoteVat.Asar,
                        d => d.NetPurchase.Asar,
                        d => d.NetVatPaidOnPurchase.Asar,
                        d => d.NetVatForTheMonth.Asar
                    );
                });
        }
    }
}
