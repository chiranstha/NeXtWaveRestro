using NextWave.Erp.Enums;
using System;
using System.Collections.Generic;

namespace NextWave.Erp.Restaurant.Dtos
{
    public class RestaurantCustomerMenuRequestDto
    {
        public int? TenantId { get; set; }
        public string TableToken { get; set; }
        public Guid? CategoryId { get; set; }
        public string Search { get; set; }
    }

    public class RestaurantCustomerMenuDto
    {
        public List<RestaurantMenuCategoryDto> Categories { get; set; } = new();
        public List<RestaurantCustomerMenuItemDto> Items { get; set; } = new();
    }

    public class RestaurantCustomerMenuItemDto
    {
        public Guid MenuItemId { get; set; }
        public Guid ProductId { get; set; }
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public decimal Price { get; set; }
        public bool IsAvailable { get; set; }
        public bool? IsVeg { get; set; }
        public int? SpiceLevel { get; set; }
        public List<RestaurantMenuVariantDto> Variants { get; set; } = new();
        public List<RestaurantModifierGroupDto> ModifierGroups { get; set; } = new();
    }

    public class RestaurantCustomerQuoteDto
    {
        public decimal GrossAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal NetAmount { get; set; }
        public decimal GrandTotal { get; set; }
        public List<RestaurantCustomerQuoteLineDto> Lines { get; set; } = new();
    }

    public class RestaurantCustomerQuoteLineDto
    {
        public Guid MenuItemId { get; set; }
        public Guid? VariantId { get; set; }
        public string ItemName { get; set; }
        public string VariantName { get; set; }
        public decimal Qty { get; set; }
        public decimal Rate { get; set; }
        public decimal ModifierTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal Amount { get; set; }
    }

    public class QuoteRestaurantCustomerOrderDto
    {
        public int? TenantId { get; set; }
        public string TableToken { get; set; }
        public List<RestaurantCustomerOrderLineDto> Lines { get; set; } = new();
    }

    public class CreateRestaurantCustomerOrderDto : QuoteRestaurantCustomerOrderDto
    {
        public RestaurantOrderType OrderType { get; set; } = RestaurantOrderType.Mobile;
        public Guid? TableId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhoneNo { get; set; }
        public string DeliveryAddress { get; set; }
        public string Notes { get; set; }
        public RestaurantCustomerPaymentMode PaymentMode { get; set; } = RestaurantCustomerPaymentMode.CounterSettlement;
        public string ClientRequestId { get; set; }
        public string StatusAccessToken { get; set; }
    }

    public class RestaurantCustomerOrderLineDto
    {
        public Guid MenuItemId { get; set; }
        public Guid? VariantId { get; set; }
        public decimal Qty { get; set; }
        public string Notes { get; set; }
        public List<RestaurantCustomerOrderModifierDto> Modifiers { get; set; } = new();
    }

    public class RestaurantCustomerOrderModifierDto
    {
        public Guid ModifierId { get; set; }
        public decimal Qty { get; set; } = 1;
    }

    public class CreateRestaurantCustomerOrderResultDto
    {
        public Guid OrderId { get; set; }
        public string OrderNo { get; set; }
        public string StatusAccessToken { get; set; }
        public RestaurantOrderStatus Status { get; set; }
        public RestaurantCustomerQuoteDto Quote { get; set; }
    }

    public class RestaurantCustomerOrderStatusDto
    {
        public Guid OrderId { get; set; }
        public string OrderNo { get; set; }
        public RestaurantOrderStatus Status { get; set; }
        public decimal GrandTotal { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? SentAt { get; set; }
        public RestaurantGuestOrderApprovalStatus? GuestApprovalStatus { get; set; }
        public string GuestRejectionReason { get; set; }
    }

    public class GetRestaurantCustomerOrderStatusDto
    {
        public Guid OrderId { get; set; }
        public string StatusAccessToken { get; set; }
    }
}
