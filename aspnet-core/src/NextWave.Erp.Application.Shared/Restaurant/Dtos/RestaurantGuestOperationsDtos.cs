using NextWave.Erp.Enums;
using System;
using System.Collections.Generic;

namespace NextWave.Erp.Restaurant.Dtos
{
    public class RestaurantTableQrDto
    {
        public Guid TableId { get; set; }
        public string TableName { get; set; }
        public string Url { get; set; }
        public string PngBase64 { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class RestaurantGuestOrderQueueDto
    {
        public Guid OrderId { get; set; }
        public string OrderNo { get; set; }
        public Guid TableId { get; set; }
        public string TableName { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal GrandTotal { get; set; }
        public List<RestaurantCustomerQuoteLineDto> Lines { get; set; } = new();
    }

    public class ReviewRestaurantGuestOrderDto
    {
        public Guid OrderId { get; set; }
        public bool Approve { get; set; }
        public string RejectionReason { get; set; }
    }

    public class RestaurantReservationDto
    {
        public Guid Id { get; set; }
        public RestaurantReservationStatus Status { get; set; }
        public bool IsWalkIn { get; set; }
        public string GuestName { get; set; }
        public string PhoneNumber { get; set; }
        public string Notes { get; set; }
        public int PartySize { get; set; }
        public DateTime StartsAt { get; set; }
        public DateTime EndsAt { get; set; }
        public Guid? TableId { get; set; }
        public string TableName { get; set; }
        public DateTime CreatedAt { get; set; }
        public string SmsStatus { get; set; }
    }

    public class RequestRestaurantReservationOtpDto
    {
        public int? TenantId { get; set; }
        public string TenancyName { get; set; }
        public string PhoneNumber { get; set; }
    }

    public class RequestRestaurantReservationOtpResultDto
    {
        public string ChallengeId { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
    }

    public class CreateRestaurantReservationRequestDto
    {
        public int? TenantId { get; set; }
        public string TenancyName { get; set; }
        public string ChallengeId { get; set; }
        public string VerificationCode { get; set; }
        public string GuestName { get; set; }
        public string PhoneNumber { get; set; }
        public string Notes { get; set; }
        public int PartySize { get; set; }
        public DateTime StartsAt { get; set; }
        public int DurationMinutes { get; set; } = 90;
        public string StatusAccessToken { get; set; }
    }

    public class CreateRestaurantReservationResultDto
    {
        public Guid Id { get; set; }
        public string StatusAccessToken { get; set; }
        public RestaurantReservationStatus Status { get; set; }
        public DateTime StartsAt { get; set; }
        public DateTime EndsAt { get; set; }
        public string Error { get; set; }
    }

    public class CreateRestaurantWalkInDto
    {
        public string GuestName { get; set; }
        public string PhoneNumber { get; set; }
        public string Notes { get; set; }
        public int PartySize { get; set; }
    }

    public class RestaurantReservationStatusDto
    {
        public Guid Id { get; set; }
        public RestaurantReservationStatus Status { get; set; }
        public DateTime StartsAt { get; set; }
        public DateTime EndsAt { get; set; }
        public string TableName { get; set; }
    }

    public class GetRestaurantReservationStatusDto
    {
        public Guid ReservationId { get; set; }
        public string StatusAccessToken { get; set; }
    }

    public class UpdateRestaurantReservationDto
    {
        public Guid Id { get; set; }
        public RestaurantReservationStatus Status { get; set; }
        public Guid? TableId { get; set; }
        public DateTime? EndsAt { get; set; }
    }

    public class ClaimRestaurantPrintJobDto
    {
        // AgentId is the stable client device ID retained for compatibility with existing web stations.
        public string AgentId { get; set; }
    }

    public class RegisterRestaurantPrintDeviceDto
    {
        public string ClientDeviceId { get; set; }
        public string Name { get; set; }
        public string Platform { get; set; }
        public List<string> RouteNames { get; set; } = new();
    }

    public class SetRestaurantPrintDeviceEnabledDto
    {
        public Guid Id { get; set; }
        public bool IsEnabled { get; set; }
    }

    public class RestaurantPrintDeviceDto
    {
        public Guid Id { get; set; }
        public string ClientDeviceId { get; set; }
        public string Name { get; set; }
        public string Platform { get; set; }
        public bool IsEnabled { get; set; }
        public DateTime LastSeenAtUtc { get; set; }
        public List<string> RouteNames { get; set; } = new();
    }

    public class RestaurantPrintRouteDto
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public bool IsActive { get; set; }
    }

    public class SaveRestaurantPrintRoutesDto
    {
        public List<RestaurantPrintRouteDto> Routes { get; set; } = new();
    }

    public class RestaurantPrintDeliveryDto
    {
        public Guid Id { get; set; }
        public Guid DeviceId { get; set; }
        public string DeviceName { get; set; }
        public string Platform { get; set; }
        public string RouteName { get; set; }
        public RestaurantPrintJobStatus Status { get; set; }
        public int Attempts { get; set; }
        public string LastError { get; set; }
        public DateTime? PrintedAtUtc { get; set; }
    }

    public class RestaurantPrintJobDto
    {
        public Guid Id { get; set; }
        public Guid? DeliveryId { get; set; }
        public Guid? LeaseToken { get; set; }
        public string ExternalJobId { get; set; }
        public RestaurantPrintJobType Type { get; set; }
        public string RouteName { get; set; }
        public string PayloadBase64 { get; set; }
        public string AgentJobId { get; set; }
        public RestaurantPrintJobStatus Status { get; set; }
        public string LastError { get; set; }
        public int Attempts { get; set; }
        public string ReprintReason { get; set; }
        public List<RestaurantPrintDeliveryDto> Deliveries { get; set; } = new();
    }

    public class ReportRestaurantPrintJobDto
    {
        public Guid Id { get; set; }
        public Guid? DeliveryId { get; set; }
        public Guid? LeaseToken { get; set; }
        public string AgentId { get; set; }
        public string AgentJobId { get; set; }
        public RestaurantPrintJobStatus Status { get; set; }
        public string Error { get; set; }
    }

    public class RetryRestaurantPrintJobDto
    {
        public Guid JobId { get; set; }
        public Guid? DeliveryId { get; set; }
    }
}
