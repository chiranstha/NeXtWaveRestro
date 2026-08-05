using Abp.Domain.Entities;
using NextWave.Erp.Accounting;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.GeneralSetting
{
    [Table("tbl_Tax")]
    public sealed class Tax : Entity<Guid>, IMayHaveTenant
    {
        public string Name { get; set; }

        public decimal Rate { get; set; }


        public string Description { get; set; }

        public bool IsActive { get; set; } = true;


        public Guid LedgerId { get; set; }

        [ForeignKey("LedgerId")] public AccountLedger AccountLedgerFk { get; set; }

        public int? TenantId { get; set; }
    }
}
