using Abp.Domain.Entities;
using NextWave.Erp.GeneralSetting;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NextWave.Erp.Enums;

namespace NextWave.Erp.Inventory
{
    [Table("tbl_Product")]
    public class Product : Entity<Guid>, IMustHaveTenant
    {
        [StringLength(200)] public string DateMiti { get; set; }
        [StringLength(200)] public virtual string ProductCode { get; set; }
        public ProductTypeEnum ProductType { get; set; }


        [StringLength(200)] public virtual string Name { get; set; }
        [StringLength(200)] public virtual string HsCode { get; set; } = "";

        public virtual decimal Mrp { get; set; }

        public virtual decimal SalesRate { get; set; }

        public virtual decimal PurchaseRate { get; set; }

        public virtual decimal MinimumStock { get; set; }

        public virtual decimal MaximumStock { get; set; }
        public virtual decimal Margin { get; set; }



        public virtual bool IsOpeningStock { get; set; }

        [StringLength(200)] public virtual string Description { get; set; }

        public virtual bool IsActive { get; set; }
        public bool IsDeleted { get; set; } = false;

        public virtual Guid TaxId { get; set; }

        [ForeignKey("TaxId")] public Tax TaxFk { get; set; }

        public virtual Guid ProductGroupId { get; set; }

        [ForeignKey("ProductGroupId")] public ProductGroup ProductGroupFk { get; set; }

        public virtual Guid UnitId { get; set; }

        [ForeignKey("UnitId")] public Unit UnitFk { get; set; }

        public int TenantId { get; set; }
    }
}
