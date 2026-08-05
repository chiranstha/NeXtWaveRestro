using Abp.Domain.Entities;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Inventory
{
    [Table("tbl_Unit")]
    public class Unit : Entity<Guid>, IMayHaveTenant
    {
        public virtual string Name { get; set; }

        public virtual string FormalName { get; set; }
        public virtual bool IsDefault { get; set; }
        public int? TenantId { get; set; }
    }
}
