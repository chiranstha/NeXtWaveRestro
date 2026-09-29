using Abp.Domain.Entities;
using NextWave.Erp.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Restaurant
{
    [Table("tbl_RestaurantStation")]
    public class RestaurantStation : Entity<Guid>, IMayHaveTenant
    {
        [StringLength(150)] public string Name { get; set; }
        [StringLength(128)] public string PrintRouteName { get; set; }
        public RestaurantStationType StationType { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; }
        public int? TenantId { get; set; }
    }
}
