using Abp.Domain.Entities;
using NextWave.Erp.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Restaurant
{
    [Table("tbl_RestaurantReservation")]
    public class RestaurantReservation : Entity<Guid>, IMayHaveTenant
    {
        public RestaurantReservationStatus Status { get; set; } = RestaurantReservationStatus.Requested;
        public bool IsWalkIn { get; set; }
        [Required, StringLength(200)] public string GuestName { get; set; }
        [Required, StringLength(30)] public string PhoneNumber { get; set; }
        [StringLength(500)] public string Notes { get; set; }
        public int PartySize { get; set; }
        public DateTime StartsAtUtc { get; set; }
        public DateTime EndsAtUtc { get; set; }
        public Guid? TableId { get; set; }
        [ForeignKey("TableId")] public RestaurantTable TableFk { get; set; }
        [StringLength(64)] public string GuestStatusTokenHash { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAtUtc { get; set; }
        public int? TenantId { get; set; }
    }
}
