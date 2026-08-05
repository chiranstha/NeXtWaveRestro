using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Configuration.Tenants.Dto
{
    public class AllTenantSettingBundleDto
    {
        public bool IsTax { get; set; }
        public bool AllowZeroValueEntry { get; set; }
        public bool ShowCurrencySymbol { get; set; }
        public bool DuplicateLedgerName { get; set; }
        public bool DuplicatePAN { get; set; }
        public bool TickPrintAfterSave { get; set; }
        public bool IsAbt { get; set; }
        public bool AutomaticProductCodeGeneration { get; set; }
        public bool Barcode { get; set; }
        public bool AllowBatchEnabled { get; set; }
        public bool AllowSize { get; set; }
        public bool AllowModelNo { get; set; }
        public bool AllowGodOwn { get; set; }
        public bool AllowRack { get; set; }
        public bool ShowSalesRate { get; set; }
        public bool ShowMrp { get; set; }
        public bool ShowPurchaseRate { get; set; }
        public bool ShowUnit { get; set; }
        public bool ShowSize { get; set; }
        public bool ShowModelNo { get; set; }
        public bool Tds { get; set; }
        public bool IsIrdSoftware { get; set; }
        public bool IsImei { get; set; }
        public bool IsCbms { get; set; }
        public string CbmsUserName { get; set; }
        public string CbmsPassword { get; set; }
        public int SalesRate { get; set; }
        public bool ShowDiscountAmount { get; set; }
        public bool ShowProdutCode { get; set; }
        public int NumberOfPrint { get; set; }
        public bool ShowBrand { get; set; }
        public bool ShowDiscountPercentage { get; set; }
        public string NegativeCashTranscation { get; set; }
        public string NegativeStockStatus { get; set; }
        public decimal RestaurantVatPercent { get; set; }
        public decimal RestaurantServiceChargePercent { get; set; }
        public bool RestaurantRequireManagerPinForSensitiveActions { get; set; }
        public bool RestaurantTicketPrintingEnabled { get; set; }
        public bool RestaurantChannelAvailabilityEnabled { get; set; }
        public string RestaurantTableWorkflow { get; set; }
        public string SalesBillFormat { get; set; }
        public bool Payroll { get; set; }
        public bool Services { get; set; }
        public bool Loan { get; set; }
        public bool Crm { get; set; }
        public bool IsEmailSent { get; set; }
        public string Transation { get; set; }
        public string StockCalculation { get; set; }
        public bool ShowSalesAdditional { get; set; }
        public bool IsLoyaltyPoint { get; set; }
    }
}
