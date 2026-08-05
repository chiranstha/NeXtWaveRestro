using Abp.Domain.Entities;
using NextWave.Erp.Accounting;
using NextWave.Erp.Inventory;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Restaurant
{
    [Table("tbl_RestaurantSupplierItemMapping")]
    public class RestaurantSupplierItemMapping : Entity<Guid>, IMayHaveTenant
    {
        public Guid ProductId { get; set; }
        [ForeignKey("ProductId")] public Product ProductFk { get; set; }

        public Guid SupplierLedgerId { get; set; }
        [ForeignKey("SupplierLedgerId")] public AccountLedger SupplierLedgerFk { get; set; }

        [StringLength(100)] public string SupplierSku { get; set; }

        public Guid UnitId { get; set; }
        [ForeignKey("UnitId")] public Unit UnitFk { get; set; }

        public decimal Rate { get; set; }
        public int LeadTimeDays { get; set; }
        public decimal MinimumOrderQty { get; set; }
        public bool IsPreferred { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public int? TenantId { get; set; }
    }
}
