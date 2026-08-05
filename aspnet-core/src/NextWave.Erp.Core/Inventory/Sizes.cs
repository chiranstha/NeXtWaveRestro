using Abp.Domain.Entities;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Inventory
{
    [Table("tbl_Sizes")]
    public class Sizes : Entity<Guid>, IMayHaveTenant
    {
        public virtual string Name { get; set; }
        public virtual string Description { get; set; }
        public virtual bool IsDefault { get; set; }
        public int? TenantId { get; set; }
    }
}
