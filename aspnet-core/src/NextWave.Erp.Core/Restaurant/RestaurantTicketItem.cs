using Abp.Domain.Entities;
using NextWave.Erp.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Restaurant
{
    [Table("tbl_RestaurantTicketItem")]
    public class RestaurantTicketItem : Entity<Guid>, IMayHaveTenant
    {
        public Guid TicketId { get; set; }
        [ForeignKey("TicketId")] public RestaurantTicket TicketFk { get; set; }
        public Guid OrderItemId { get; set; }
        [ForeignKey("OrderItemId")] public RestaurantOrderItem OrderItemFk { get; set; }
        public decimal Qty { get; set; }
        public RestaurantOrderItemStatus Status { get; set; } = RestaurantOrderItemStatus.Sent;
        [StringLength(500)] public string CancelReason { get; set; }
        public int? TenantId { get; set; }
    }
}
