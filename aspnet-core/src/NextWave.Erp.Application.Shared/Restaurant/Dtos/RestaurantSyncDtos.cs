using NextWave.Erp.Enums;
using System;
using System.Collections.Generic;

namespace NextWave.Erp.Restaurant.Dtos
{
    public class RegisterRestaurantPushTokenDto
    {
        public Guid DeviceId { get; set; }
        public string Platform { get; set; }
        public string Token { get; set; }
    }

    public class PullRestaurantChangesDto
    {
        public long SinceSeq { get; set; }
        public Guid? DeviceId { get; set; }
        public int MaxResultCount { get; set; } = 500;
        public List<RestaurantSyncEntityType> EntityTypes { get; set; } = new();
    }

    public class RestaurantChangeDto
    {
        public long Seq { get; set; }
        public RestaurantSyncEntityType EntityType { get; set; }
        public RestaurantSyncOperation Operation { get; set; }
        public string EntityId { get; set; }
        public string PayloadJson { get; set; }
        public Guid? ChangedByDeviceId { get; set; }
        public DateTime ChangedAt { get; set; }
    }

    public class PullRestaurantChangesResultDto
    {
        public long LastSeq { get; set; }
        public List<RestaurantChangeDto> Changes { get; set; } = new();
    }

    public class UploadRestaurantSyncBatchDto
    {
        public Guid? DeviceId { get; set; }
        public string BatchGuid { get; set; }
        public List<UploadRestaurantOrderDto> Orders { get; set; } = new();
    }

    public class UploadRestaurantSyncBatchResultDto
    {
        public string BatchGuid { get; set; }
        public RestaurantSyncUploadStatus Status { get; set; }
        public List<Guid> ServerOrderIds { get; set; } = new();
        public string ErrorMessage { get; set; }
    }

    public class AcknowledgeRestaurantChangesDto
    {
        public Guid DeviceId { get; set; }
        public long LastSeq { get; set; }
    }
}
