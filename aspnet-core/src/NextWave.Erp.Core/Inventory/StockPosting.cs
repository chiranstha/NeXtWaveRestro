using Abp.Domain.Entities;
using NextWave.Erp.Accounting;
using NextWave.Erp.ControlPanel;
using NextWave.Erp.GeneralSetting;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Inventory
{
    [Table("tbl_StockPosting")]
    public sealed class StockPosting : Entity<Guid>, IMayHaveTenant
    {
        public int VoucherNumbering { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime Date { get; set; }
        public string DateMiti { get; set; }
        public Guid VoucherTypeId { get; set; }
        [ForeignKey("VoucherTypeId")] public VoucherType VoucherTypeFk { get; set; }
        public string VoucherNo { get; set; }

        public decimal GrossAmount { get; set; } = 0;
        public decimal DiscountAmount { get; set; } = 0;
        public decimal NetAmount { get; set; } = 0;
        public decimal Amount { get; set; } = 0;
        public decimal TaxAmount { get; set; } = 0;

        public Guid ProductId { get; set; }
        [ForeignKey("ProductId")] public Product ProductFk { get; set; }


        public Guid UnitId { get; set; }

        [ForeignKey("UnitId")] public Unit UnitFk { get; set; }

        public Guid? LedgerId { get; set; }

        [ForeignKey("LedgerId")] public AccountLedger AccountLedgerFk { get; set; }

        public Guid AgainstVoucherTypeId { get; set; }
        public string AgainstVoucherNo { get; set; }
        public decimal InWardQty { get; set; }
        public decimal OutWardQty { get; set; }
        public decimal Rate { get; set; }
        public bool IsValueIncrease { get; set; } = true;
        public Guid FinancialYearId { get; set; }

        public string VendorVoucherNo { get; set; } = "";

        public Guid? SourceDetailId { get; set; }

        public Guid? MasterId { get; set; }
        public int? TenantId { get; set; }
    }
}
