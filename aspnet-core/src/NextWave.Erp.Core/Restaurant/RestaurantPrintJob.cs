using Abp.Domain.Entities;
using NextWave.Erp.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Restaurant
{
    [Table("tbl_RestaurantPrintJob")]
    public class RestaurantPrintJob : Entity<Guid>, IMayHaveTenant
    {
        [Required, StringLength(120)] public string ExternalJobId { get; set; }
        public RestaurantPrintJobType Type { get; set; }
        public RestaurantPrintJobStatus Status { get; set; } = RestaurantPrintJobStatus.Pending;
        public Guid? TicketId { get; set; }
        public Guid? OrderId { get; set; }
        public Guid? StationId { get; set; }
        [Required, StringLength(128)] public string RouteName { get; set; }
        [Required] public byte[] Payload { get; set; }
        [StringLength(120)] public string LeaseOwner { get; set; }
        public DateTime? LeaseUntilUtc { get; set; }
        public int Attempts { get; set; }
        [StringLength(500)] public string LastError { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? PrintedAtUtc { get; set; }
        public int? TenantId { get; set; }
    }
}
