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
    [Table("tbl_PurchaseReturn")]
    public class PurchaseReturn : Entity<Guid>, IMayHaveTenant
    {
        public virtual int VoucherNumbering { get; set; }
        public virtual string VoucherNo { get; set; }

        public virtual string DateMiti { get; set; }

        public virtual DateTime Date { get; set; }

        public virtual string Description { get; set; }

        public virtual Guid PurchaseAccount { get; set; }

        public virtual decimal TotalAmount { get; set; }

        public virtual decimal TotalDiscount { get; set; }

        public virtual decimal NetAmount { get; set; }
        public bool DebitOrCreditNote { get; set; }

        public virtual decimal TotalTaxableAmount { get; set; }


        public virtual decimal TotalTax { get; set; }

        public virtual decimal GrandTotal { get; set; }

        public virtual string LrNo { get; set; }
        public virtual string TransportationCompany { get; set; }
        public InvoiceTypeEnum InvoiceType { get; set; }
        public virtual Guid FinancialYearId { get; set; }

        public virtual Guid VoucherTypeId { get; set; }
        [ForeignKey("VoucherTypeId")] public VoucherType VoucherTypeFk { get; set; }


        public virtual Guid? PurchaseMasterId { get; set; }

        [ForeignKey("PurchaseMasterId")] public PurchaseMaster PurchaseMasterFk { get; set; }

        public virtual Guid LedgerId { get; set; }

        [ForeignKey("LedgerId")] public AccountLedger AccountLedgerFk { get; set; }

        public virtual long? CreateUserId { get; set; }
        [ForeignKey("CreateUserId")] public User CreateUserFk { get; set; }

        public virtual long? UpdateUserId { get; set; }

        [ForeignKey("UpdateUserId")] public User UpdateUserFk { get; set; }
        public virtual decimal PostingNumber { get; set; }
        public int? TenantId { get; set; }
        public virtual ReturnType ReturnType { get; set; }
        public virtual Guid ReturnTaxId { get; set; }
    }
}
