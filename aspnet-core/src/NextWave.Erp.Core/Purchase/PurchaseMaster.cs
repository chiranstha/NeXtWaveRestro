using Abp.Domain.Entities;
using NextWave.Erp.Accounting;
using NextWave.Erp.Authorization.Users;
using NextWave.Erp.ControlPanel;
using NextWave.Erp.Enums;
using NextWave.Erp.GeneralSetting;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Purchase
{
    [Table("tbl_PurchaseMaster")]
    public class PurchaseMaster : Entity<Guid>, IMayHaveTenant
    {
        public virtual string VoucherNo { get; set; }
        public virtual int VoucherNumbering { get; set; }

        public virtual DateTime Date { get; set; }

        public virtual string DateMiti { get; set; }

        public virtual DateTime? CreditDate { get; set; }

        public virtual string VendorInvoiceNo { get; set; }

        //public virtual DateTime? VendorInvoiceDate { get; set; }

        public virtual int CreditPeriod { get; set; }
        //public virtual InvoiceTypeEnum InvoiceTypeEnum { get; set; }

        public virtual string Narration { get; set; }
        //public virtual decimal? TotalCustomAmount { get; set; }

        public virtual decimal TotalTax { get; set; }
        public virtual decimal TotalTaxableAmount { get; set; }

        //public virtual string PpdNo { get; set; }
        //public virtual Guid? CustomLedgerId { get; set; }
        public virtual decimal TotalAmount { get; set; }

        public virtual decimal BillDiscount { get; set; }

        public virtual decimal GrandTotal { get; set; }
        public virtual PurchaseModeType AgainstId { get; set; }
        public virtual string LrNo { get; set; }
        //public virtual string TransportationCompany { get; set; }

        public virtual Guid VoucherTypeId { get; set; }
        [ForeignKey("VoucherTypeId")] public VoucherType VoucherTypeFk { get; set; }


        public virtual Guid PurchaseAccountId { get; set; }

        public virtual Guid FinancialYearId { get; set; }

        public virtual Guid LedgerId { get; set; }

        [ForeignKey("LedgerId")] public AccountLedger AccountLedgerFk { get; set; }

        public virtual Guid? PurchaseOrderMasterId { get; set; }

        [ForeignKey("PurchaseOrderMasterId")] public PurchaseOrderMaster PurchaseOrderMasterFk { get; set; }


        public virtual long? CreateUserId { get; set; }
        [ForeignKey("CreateUserId")] public User CreateUserFk { get; set; }

        public virtual long? UpdateUserId { get; set; }

        [ForeignKey("UpdateUserId")] public User UpdateUserFk { get; set; }
        public int? TenantId { get; set; }
        public decimal? PostingNumbering { get; set; }
    }
}
