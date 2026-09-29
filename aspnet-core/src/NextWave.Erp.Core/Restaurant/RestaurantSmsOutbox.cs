using Abp.Domain.Entities;
using NextWave.Erp.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Restaurant
{
    [Table("tbl_RestaurantSmsOutbox")]
    public class RestaurantSmsOutbox : Entity<Guid>, IMayHaveTenant
    {
        [Required, StringLength(30)] public string PhoneNumber { get; set; }
        [Required, StringLength(1000)] public string Message { get; set; }
        public RestaurantSmsOutboxStatus Status { get; set; } = RestaurantSmsOutboxStatus.Pending;
        public int Attempts { get; set; }
        [StringLength(500)] public string LastError { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? SentAtUtc { get; set; }
        public DateTime? NextAttemptAtUtc { get; set; }
        public int? TenantId { get; set; }
    }
}
