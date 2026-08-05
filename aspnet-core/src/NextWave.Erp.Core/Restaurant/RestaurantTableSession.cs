using Abp.Domain.Entities;
using NextWave.Erp.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Restaurant
{
    [Table("tbl_RestaurantTableSession")]
    public class RestaurantTableSession : Entity<Guid>, IMayHaveTenant
    {
        public Guid? TableId { get; set; }
        [ForeignKey("TableId")] public RestaurantTable TableFk { get; set; }
        [StringLength(50)] public string SessionNo { get; set; }
        public RestaurantOrderStatus Status { get; set; } = RestaurantOrderStatus.Draft;
        public DateTime OpenedAt { get; set; } = DateTime.Now;
        public DateTime? ClosedAt { get; set; }
        public int GuestCount { get; set; }
        public long? WaiterUserId { get; set; }
        [StringLength(200)] public string CustomerName { get; set; }
        [StringLength(50)] public string CustomerPhoneNo { get; set; }
        public int? TenantId { get; set; }
    }
}
