using Abp.Domain.Entities;
using NextWave.Erp.Enums;
using NextWave.Erp.Sales;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextWave.Erp.Restaurant
{
    [Table("tbl_RestaurantRefund")]
    public class RestaurantRefund : Entity<Guid>, IMayHaveTenant
    {
        public int? TenantId { get; set; }
        public Guid RestaurantOrderId { get; set; }
        [ForeignKey(nameof(RestaurantOrderId))] public RestaurantOrder OrderFk { get; set; }
        public Guid SalesMasterId { get; set; }
        [ForeignKey(nameof(SalesMasterId))] public SalesMaster SalesMasterFk { get; set; }
        public Guid? SalesReturnMasterId { get; set; }
        [ForeignKey(nameof(SalesReturnMasterId))] public SalesReturnMaster SalesReturnMasterFk { get; set; }
        public Guid? TipSalesReturnMasterId { get; set; }
        [ForeignKey(nameof(TipSalesReturnMasterId))] public SalesReturnMaster TipSalesReturnMasterFk { get; set; }
        public Guid? CreditNoteSalesReturnMasterId { get; set; }
        [ForeignKey(nameof(CreditNoteSalesReturnMasterId))] public SalesReturnMaster CreditNoteSalesReturnMasterFk { get; set; }
        [Required, StringLength(100)] public string ClientRequestId { get; set; }
        [Required, StringLength(64)] public string RequestHash { get; set; }
        [Required, StringLength(500)] public string Reason { get; set; }
        public decimal ItemRefundAmount { get; set; }
        public decimal TipRefundAmount { get; set; }
        public decimal CreditNoteAmount { get; set; }
        public decimal PayoutAmount { get; set; }
        public decimal SettledAmount { get; set; }
        public RestaurantRefundStatus Status { get; set; }
        public long ApprovedByUserId { get; set; }
        public DateTime ApprovedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? SettledAt { get; set; }
    }

    [Table("tbl_RestaurantRefundLine")]
    public class RestaurantRefundLine : Entity<Guid>, IMayHaveTenant
    {
        public int? TenantId { get; set; }
        public Guid RefundId { get; set; }
        [ForeignKey(nameof(RefundId))] public RestaurantRefund RefundFk { get; set; }
        public Guid SalesDetailId { get; set; }
        public Guid OrderItemId { get; set; }
        public decimal Qty { get; set; }
        public decimal GrossAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal NetAmount { get; set; }
        public decimal RestockQty { get; set; }
        public RestaurantRefundStockDisposition StockDisposition { get; set; }
    }

    [Table("tbl_RestaurantRefundTender")]
    public class RestaurantRefundTender : Entity<Guid>, IMayHaveTenant
    {
        public int? TenantId { get; set; }
        public Guid RefundId { get; set; }
        [ForeignKey(nameof(RefundId))] public RestaurantRefund RefundFk { get; set; }
        public Guid? OriginalTenderId { get; set; }
        public Guid? OriginalBillPaymentId { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public Guid PaymentLedgerId { get; set; }
        public decimal AllocatedAmount { get; set; }
        public decimal SettledAmount { get; set; }
    }

    [Table("tbl_RestaurantRefundSettlement")]
    public class RestaurantRefundSettlement : Entity<Guid>, IMayHaveTenant
    {
        public int? TenantId { get; set; }
        public Guid RefundId { get; set; }
        [ForeignKey(nameof(RefundId))] public RestaurantRefund RefundFk { get; set; }
        [Required, StringLength(100)] public string ClientRequestId { get; set; }
        [Required, StringLength(64)] public string RequestHash { get; set; }
        public decimal Amount { get; set; }
        public long SettledByUserId { get; set; }
        public DateTime SettledAt { get; set; }
        public Guid? CashShiftId { get; set; }
        [ForeignKey(nameof(CashShiftId))] public RestaurantCashShift CashShiftFk { get; set; }
    }

    [Table("tbl_RestaurantRefundSettlementTender")]
    public class RestaurantRefundSettlementTender : Entity<Guid>, IMayHaveTenant
    {
        public int? TenantId { get; set; }
        public Guid SettlementId { get; set; }
        [ForeignKey(nameof(SettlementId))] public RestaurantRefundSettlement SettlementFk { get; set; }
        public Guid RefundTenderId { get; set; }
        [ForeignKey(nameof(RefundTenderId))] public RestaurantRefundTender RefundTenderFk { get; set; }
        public decimal Amount { get; set; }
        [StringLength(200)] public string Reference { get; set; }
        public Guid? PaymentMasterId { get; set; }
    }

    [Table("tbl_RestaurantClientOperation")]
    public class RestaurantClientOperation : Entity<Guid>, IMayHaveTenant
    {
        public int? TenantId { get; set; }
        public long UserId { get; set; }
        [Required, StringLength(100)] public string ClientRequestId { get; set; }
        [Required, StringLength(80)] public string OperationType { get; set; }
        [Required, StringLength(64)] public string RequestHash { get; set; }
        public Guid? EntityId { get; set; }
        [StringLength(4000)] public string ResultJson { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    [Table("tbl_RestaurantSetupAcknowledgement")]
    public class RestaurantSetupAcknowledgement : Entity<Guid>, IMayHaveTenant
    {
        public int? TenantId { get; set; }
        public RestaurantSetupCheckKey CheckKey { get; set; }
        public long CompletedByUserId { get; set; }
        public DateTime CompletedAt { get; set; }
        [StringLength(500)] public string Note { get; set; }
    }
}
