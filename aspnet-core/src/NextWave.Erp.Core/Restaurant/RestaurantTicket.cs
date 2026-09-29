using Abp.Domain.Entities;
using NextWave.Erp.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Restaurant
{
    [Table("tbl_RestaurantTicket")]
    public class RestaurantTicket : Entity<Guid>, IMayHaveTenant
    {
        [StringLength(50)] public string TicketNo { get; set; }
        public Guid OrderId { get; set; }
        [ForeignKey("OrderId")] public RestaurantOrder OrderFk { get; set; }
        public Guid StationId { get; set; }
        [ForeignKey("StationId")] public RestaurantStation StationFk { get; set; }
        public RestaurantTicketType TicketType { get; set; }
        public RestaurantTicketPurpose Purpose { get; set; } = RestaurantTicketPurpose.NewOrder;
        public RestaurantTicketStatus Status { get; set; } = RestaurantTicketStatus.Pending;
        public DateTime SentAt { get; set; } = DateTime.Now;
        public DateTime? StartedAt { get; set; }
        public DateTime? ReadyAt { get; set; }
        public DateTime? ServedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public DateTime? PrintedAt { get; set; }
        public DateTime? LastPrintedAt { get; set; }
        public DateTime? PrintRequestedAt { get; set; }
        public DateTime? LastPrintConfirmedAt { get; set; }
        public int PrintCount { get; set; }
        [StringLength(500)] public string CancelReason { get; set; }
        public int? TenantId { get; set; }
    }
}
