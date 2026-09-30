using Abp.Domain.Entities;
using NextWave.Erp.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Restaurant
{
    [Table("tbl_RestaurantPrintDelivery")]
    public class RestaurantPrintDelivery : Entity<Guid>, IMayHaveTenant
    {
        public Guid PrintJobId { get; set; }
        public Guid DeviceId { get; set; }
        [Required, StringLength(128)] public string RouteName { get; set; }
        public RestaurantPrintJobStatus Status { get; set; } = RestaurantPrintJobStatus.Pending;
        [StringLength(120)] public string LeaseOwner { get; set; }
        public Guid? LeaseToken { get; set; }
        [StringLength(120)] public string AgentJobId { get; set; }
        public DateTime? LeaseUntilUtc { get; set; }
        public int Attempts { get; set; }
        [StringLength(500)] public string LastError { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? PrintedAtUtc { get; set; }
        public int? TenantId { get; set; }
    }
}
