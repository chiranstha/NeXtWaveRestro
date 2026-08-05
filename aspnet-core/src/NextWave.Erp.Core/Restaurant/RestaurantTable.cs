using Abp.Domain.Entities;
using NextWave.Erp.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Restaurant
{
    [Table("tbl_RestaurantTable")]
    public class RestaurantTable : Entity<Guid>, IMayHaveTenant
    {
        [StringLength(100)] public string Name { get; set; }
        [StringLength(50)] public string Code { get; set; }
        public int Capacity { get; set; }
        public int SortOrder { get; set; }
        public RestaurantTableStatus Status { get; set; } = RestaurantTableStatus.Available;
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; }
        public Guid AreaId { get; set; }
        [ForeignKey("AreaId")] public RestaurantArea AreaFk { get; set; }
        public int? TenantId { get; set; }
    }
}
