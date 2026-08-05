using Abp.Domain.Entities;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Inventory
{
    [Table("tbl_Bom")]
    public class Bom : Entity<Guid>, IMayHaveTenant
    {
        public bool IsDeleted { get; set; } = false;
        public virtual Guid ProductId { get; set; }

        [ForeignKey("ProductId")] public Product ProductFk { get; set; }

        public virtual Guid RawMaterialId { get; set; }
        [ForeignKey("RawMaterialId")] public Product RawMaterialFk { get; set; }

        public virtual decimal Quantity { get; set; }

        public virtual Guid UnitId { get; set; }

        [ForeignKey("UnitId")] public Unit UnitFk { get; set; }
        public virtual decimal WastagePercentage { get; set; }
        public virtual decimal CostRate { get; set; }
        public virtual bool IsActive { get; set; } = true;
        public DateTime Date { get; set; } = DateTime.UtcNow;
        public int? TenantId { get; set; }
    }
}
