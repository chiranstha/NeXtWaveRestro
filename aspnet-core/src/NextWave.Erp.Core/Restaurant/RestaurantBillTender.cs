using Abp.Domain.Entities;
using NextWave.Erp.Enums;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Restaurant
{
    [Table("tbl_RestaurantBillTender")]
    public class RestaurantBillTender : Entity<Guid>, IMayHaveTenant
    {
        public Guid BillPaymentId { get; set; }
        [ForeignKey(nameof(BillPaymentId))] public RestaurantBillPayment BillPaymentFk { get; set; }
        public Guid? CashShiftId { get; set; }
        [ForeignKey(nameof(CashShiftId))] public RestaurantCashShift CashShiftFk { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public Guid? PaymentLedgerId { get; set; }
        public decimal Amount { get; set; }
        public decimal ReceivedAmount { get; set; }
        public decimal ChangeAmount { get; set; }
        public string Reference { get; set; }
        public int? TenantId { get; set; }
    }
}
