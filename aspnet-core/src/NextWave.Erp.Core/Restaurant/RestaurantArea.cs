using Abp.Domain.Entities;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Restaurant
{
    [Table("tbl_RestaurantArea")]
    public class RestaurantArea : Entity<Guid>, IMayHaveTenant
    {
        [StringLength(200)] public string Name { get; set; }
        [StringLength(500)] public string Description { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; }
        public int? TenantId { get; set; }
    }
}
