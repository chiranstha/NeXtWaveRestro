using Abp.Domain.Entities;
using NextWave.Erp.Authorization.Users;
using NextWave.Erp.Enums;
using NextWave.Erp.GeneralSetting;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Restaurant
{
    [Table("tbl_RestaurantStockAdjustment")]
    public class RestaurantStockAdjustment : Entity<Guid>, IMayHaveTenant
    {
        [StringLength(50)] public string VoucherNo { get; set; }
        public DateTime Date { get; set; }
        [StringLength(20)] public string DateMiti { get; set; }
        public RestaurantStockAdjustmentType AdjustmentType { get; set; }
        [StringLength(500)] public string Description { get; set; }
        public Guid VoucherTypeId { get; set; }
        [ForeignKey("VoucherTypeId")] public VoucherType VoucherTypeFk { get; set; }
        public int VoucherNumbering { get; set; }
        public Guid FinancialYearId { get; set; }
        public long? CreateUserId { get; set; }
        [ForeignKey("CreateUserId")] public User CreateUserFk { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsDeleted { get; set; }
        public int? TenantId { get; set; }
        public ICollection<RestaurantStockAdjustmentLine> Lines { get; set; } = new List<RestaurantStockAdjustmentLine>();
    }
}
