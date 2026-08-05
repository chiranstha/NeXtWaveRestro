using Abp.Domain.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextWave.Erp.Inventory
{

    [Table("tbl_ProductGroup")]
    public class ProductGroup : Entity<Guid>, IMayHaveTenant
    {
        public virtual string Name { get; set; }


        public virtual Guid? GroupUnder { get; set; }

        [ForeignKey("GroupUnder")] public ProductGroup ProductGroupFk { get; set; }


        public virtual string Description { get; set; }

        public virtual bool IsDefult { get; set; }
        public int? TenantId { get; set; }
    }
}
