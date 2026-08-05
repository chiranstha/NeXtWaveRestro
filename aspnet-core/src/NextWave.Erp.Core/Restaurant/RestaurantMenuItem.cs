using Abp.Domain.Entities;
using NextWave.Erp.Inventory;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Restaurant
{
    [Table("tbl_RestaurantMenuItem")]
    public class RestaurantMenuItem : Entity<Guid>, IMayHaveTenant
    {
        public Guid CategoryId { get; set; }
        [ForeignKey("CategoryId")] public RestaurantMenuCategory CategoryFk { get; set; }
        public Guid ProductId { get; set; }
        [ForeignKey("ProductId")] public Product ProductFk { get; set; }
        public Guid? StationId { get; set; }
        [ForeignKey("StationId")] public RestaurantStation StationFk { get; set; }
        [StringLength(200)] public string DisplayName { get; set; }
        [StringLength(40)] public string ShortCode { get; set; }
        [StringLength(500)] public string Description { get; set; }
        [StringLength(20)] public string ColorHex { get; set; }
        [StringLength(512)] public string ImageUrl { get; set; }
        public decimal Price { get; set; }
        public int PreparationMinutes { get; set; }
        public int SortOrder { get; set; }
        public bool IsAvailable { get; set; } = true;
        public DateTime? UnavailableUntil { get; set; }
        public bool? IsVeg { get; set; }
        public int? SpiceLevel { get; set; }
        public bool IsFeatured { get; set; }
        public bool HasVariants { get; set; }
        public bool HasModifiers { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; }
        public int? TenantId { get; set; }
    }
}
