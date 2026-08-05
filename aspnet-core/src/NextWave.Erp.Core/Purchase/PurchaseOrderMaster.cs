using Abp.Domain.Entities;
using NextWave.Erp.Accounting;
using NextWave.Erp.Authorization.Users;
using NextWave.Erp.ControlPanel;
using NextWave.Erp.GeneralSetting;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Purchase
{
    [Table("tbl_PurchaseOrderMaster")]
    public class PurchaseOrderMaster : Entity<Guid>, IMayHaveTenant
    {
        public virtual int VoucherNumbering { get; set; }
        public virtual string VoucherNo { get; set; }

        public virtual DateTime Date { get; set; }

        public virtual string DateMiti { get; set; }

        public virtual DateTime? DueDate { get; set; }

        public virtual string DueDateMiti { get; set; }

        public virtual bool Cancelled { get; set; }


        public virtual string Description { get; set; }

        public virtual decimal? TotalAmount { get; set; }

        public virtual Guid VoucherTypeId { get; set; }
        [ForeignKey("VoucherTypeId")] public VoucherType VoucherTypeFk { get; set; }

        public bool IsDeleted { get; set; } = false;

        public Guid FinancialYearId { get; set; }

        public bool IsCompleted { get; set; }

        public virtual Guid LedgerId { get; set; }

        [ForeignKey("LedgerId")] public AccountLedger AccountLedgerFk { get; set; }

        public virtual long? CreateUserId { get; set; }

        [ForeignKey("CreateUserId")] public User CreateUserFk { get; set; }

        public virtual long? UpdateUserId { get; set; }

        [ForeignKey("UpdateUserId")] public User UpdateUserFk { get; set; }
        public virtual decimal? PostingNumbering { get; set; }
        public int? TenantId { get; set; }
    }
}
