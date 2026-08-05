using Abp.Domain.Entities;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Inventory
{
    [Table("tbl_UnitConversion")]
    public class UnitConversion : Entity<Guid>, IMayHaveTenant
    {
        public bool IsDeleted { get; set; } = false;

        [Column(TypeName = "decimal(18,8)")] public virtual decimal ConversionRate { get; set; }

        public virtual decimal Qty { get; set; }

        public virtual decimal PrimaryQty { get; set; }

        public virtual Guid ProductId { get; set; }

        [ForeignKey("ProductId")] public Product ProductFk { get; set; }

        public virtual Guid UnitId { get; set; }

        [ForeignKey("UnitId")] public Unit UnitFk { get; set; }

        public int? TenantId { get; set; }
    }
}
