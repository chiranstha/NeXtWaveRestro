using Abp.Domain.Entities;
using NextWave.Erp.Inventory;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Sales
{
    [Table("tbl_SalesProductCancelDetail")]
    public class SalesProductCancelDetail : Entity<Guid>, IMayHaveTenant
    {
        public virtual Guid ProductId { get; set; }
        [ForeignKey("ProductId")] public Product ProductFk { get; set; }

        public decimal Qty { get; set; }

        public virtual Guid SalesProductCancelMasterId { get; set; }

        [ForeignKey("SalesProductCancelMasterId")]
        public SalesProductCancelMaster SalesProductCancelMasterFk { get; set; }

        public int? TenantId { get; set; }
    }
}
