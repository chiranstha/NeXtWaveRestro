using Abp.Domain.Entities;
using JetBrains.Annotations;
using NextWave.Erp.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.ControlPanel
{
    [Table("tbl_Branch")]
    public class Branch : Entity<Guid>, IMayHaveTenant
    {
        [Required]
        public virtual string Name { get; set; }
        public virtual string CompanyName { get; set; }
        public virtual string PANumber { get; set; }
        public virtual string BranchCode { get; set; }
        public virtual string Address { get; set; }
        public virtual StateEnum State { get; set; }
        public virtual string PhoneNo1 { get; set; }
        public virtual string Email { get; set; }
        public virtual string PhoneNo2 { get; set; }
        public virtual string Description { get; set; }
        public virtual bool Status { get; set; }
        [CanBeNull] public virtual byte[] Image1 { get; set; }

        public virtual BranchType BranchType { get; set; }

        public virtual Guid? BranchId { get; set; }

        [ForeignKey("BranchId")] public Branch BranchFk { get; set; }


        public bool IsMain { get; set; } = false;

        public int? TenantId { get; set; }
    }
}
