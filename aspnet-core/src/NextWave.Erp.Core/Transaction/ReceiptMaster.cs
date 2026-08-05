using Abp.Domain.Entities;
using NextWave.Erp.Accounting;
using NextWave.Erp.Authorization.Users;
using NextWave.Erp.ControlPanel;
using NextWave.Erp.GeneralSetting;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Transaction
{
    [Table("tbl_ReceiptMaster")]
    public class ReceiptMaster : Entity<Guid>, IMayHaveTenant
    {
        public virtual int VoucherNumbering { get; set; }
        public virtual string VoucherNo { get; set; }

        public virtual string RefVoucherNo { get; set; }
        public virtual Guid? RefVoucherTypeId { get; set; }

        public virtual DateTime Date { get; set; }

        public virtual string DateMiti { get; set; }

        public virtual decimal TotalAmount { get; set; }


        public virtual string Description { get; set; }

        public virtual Guid VoucherTypeId { get; set; }
        [ForeignKey("VoucherTypeId")] public VoucherType VoucherTypeFk { get; set; }


        public virtual Guid FinancialYearId { get; set; }

        [ForeignKey("FinancialYearId")] public FinancialYear FinancialYearFk { get; set; }

        public virtual Guid LedgerId { get; set; }

        [ForeignKey("LedgerId")] public AccountLedger AccountLedgerFk { get; set; }

        public virtual long? CreateUserId { get; set; }
        [ForeignKey("CreateUserId")] public User CreateUserFk { get; set; }

        public virtual long? UpdateUserId { get; set; }

        public virtual decimal? PostingNumbering { get; set; }

        [ForeignKey("UpdateUserId")] public User UpdateUserFk { get; set; }
        public int? TenantId { get; set; }
    }
}
