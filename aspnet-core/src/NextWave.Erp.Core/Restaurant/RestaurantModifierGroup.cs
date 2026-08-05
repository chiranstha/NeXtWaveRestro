using Abp.Domain.Entities;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Restaurant
{
    [Table("tbl_RestaurantModifierGroup")]
    public class RestaurantModifierGroup : Entity<Guid>, IMayHaveTenant
    {
        [StringLength(120)] public string Name { get; set; }
        public int MinSelect { get; set; }
        public int MaxSelect { get; set; } = 1;
        public bool IsRequired { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; }
        public int? TenantId { get; set; }
    }
}
