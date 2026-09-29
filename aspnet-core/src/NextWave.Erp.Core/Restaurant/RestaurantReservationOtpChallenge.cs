using Abp.Domain.Entities;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Restaurant
{
    [Table("tbl_RestaurantReservationOtpChallenge")]
    public class RestaurantReservationOtpChallenge : Entity<Guid>, IMayHaveTenant
    {
        [Required, StringLength(30)] public string PhoneNumber { get; set; }
        [Required, StringLength(64)] public string CodeHash { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
        public DateTime? ConsumedAtUtc { get; set; }
        public int Attempts { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public int? TenantId { get; set; }
    }
}
