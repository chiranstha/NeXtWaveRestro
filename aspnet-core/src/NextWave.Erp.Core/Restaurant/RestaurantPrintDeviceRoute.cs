using Abp.Domain.Entities;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Restaurant
{
    [Table("tbl_RestaurantPrintDeviceRoute")]
    public class RestaurantPrintDeviceRoute : Entity<Guid>, IMayHaveTenant
    {
        public Guid DeviceId { get; set; }
        [Required, StringLength(128)] public string RouteName { get; set; }
        public int? TenantId { get; set; }
    }
}
