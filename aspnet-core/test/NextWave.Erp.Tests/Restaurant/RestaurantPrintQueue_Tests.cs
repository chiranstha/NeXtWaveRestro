using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Uow;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using NextWave.Erp.Enums;
using NextWave.Erp.Restaurant;
using NextWave.Erp.Restaurant.Dtos;
using Shouldly;
using Xunit;

namespace NextWave.Erp.Tests.Restaurant;

public class RestaurantPrintQueue_Tests : AppTestBase
{
    private readonly IRestaurantGuestOperationsAppService _printService;
    private readonly RestaurantPrintQueueService _queueService;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public RestaurantPrintQueue_Tests()
    {
        _printService = Resolve<IRestaurantGuestOperationsAppService>();
        _queueService = Resolve<RestaurantPrintQueueService>();
        _unitOfWorkManager = Resolve<IUnitOfWorkManager>();
    }

    [Fact]
    public async Task Matching_route_creates_one_delivery_per_device_and_stale_lease_is_rejected()
    {
        var tenantId = AbpSession.TenantId.Value;
        var windowsDeviceId = Guid.NewGuid();
        var androidDeviceId = Guid.NewGuid();
        var secondAndroidDeviceId = Guid.NewGuid();
        var unrelatedDeviceId = Guid.NewGuid();
        var jobId = Guid.NewGuid();

        await UsingDbContextAsync(async context =>
        {
            context.RestaurantPrintRoutes.AddRange(
                new RestaurantPrintRoute { TenantId = tenantId, Name = "kitchen", DisplayName = "Kitchen", IsActive = true },
                new RestaurantPrintRoute { TenantId = tenantId, Name = "bar", DisplayName = "Bar", IsActive = true });
            context.RestaurantPrintDevices.AddRange(
                new RestaurantPrintDevice { Id = windowsDeviceId, TenantId = tenantId, ClientDeviceId = "windows-kitchen", Name = "Windows station", Platform = "Windows", IsEnabled = true },
                new RestaurantPrintDevice { Id = androidDeviceId, TenantId = tenantId, ClientDeviceId = "android-kitchen", Name = "Kitchen phone", Platform = "Android", IsEnabled = true },
                new RestaurantPrintDevice { Id = secondAndroidDeviceId, TenantId = tenantId, ClientDeviceId = "android-kitchen-2", Name = "Kitchen phone 2", Platform = "Android", IsEnabled = true },
                new RestaurantPrintDevice { Id = unrelatedDeviceId, TenantId = tenantId, ClientDeviceId = "android-bar", Name = "Bar phone", Platform = "Android", IsEnabled = true });
            context.RestaurantPrintDeviceRoutes.AddRange(
                new RestaurantPrintDeviceRoute { TenantId = tenantId, DeviceId = windowsDeviceId, RouteName = "kitchen" },
                new RestaurantPrintDeviceRoute { TenantId = tenantId, DeviceId = androidDeviceId, RouteName = "kitchen" },
                new RestaurantPrintDeviceRoute { TenantId = tenantId, DeviceId = secondAndroidDeviceId, RouteName = "kitchen" },
                new RestaurantPrintDeviceRoute { TenantId = tenantId, DeviceId = unrelatedDeviceId, RouteName = "bar" });
        });

        using (var unitOfWork = _unitOfWorkManager.Begin())
        {
            await _queueService.QueueAsync(new RestaurantPrintJob
            {
                Id = jobId,
                TenantId = tenantId,
                ExternalJobId = "ticket-kitchen-100",
                Type = RestaurantPrintJobType.KitchenTicket,
                RouteName = "kitchen",
                Payload = new byte[] { 0x1b, 0x40 },
                Status = RestaurantPrintJobStatus.Pending
            }, tenantId);
            await unitOfWork.CompleteAsync();
        }

        var deliveries = await UsingDbContextAsync(context => context.RestaurantPrintDeliveries
            .Where(delivery => delivery.TenantId == tenantId && delivery.PrintJobId == jobId)
            .ToListAsync());
        deliveries.Count.ShouldBe(3);
        deliveries.Select(delivery => delivery.DeviceId).OrderBy(id => id)
            .ShouldBe(new[] { windowsDeviceId, androidDeviceId, secondAndroidDeviceId }.OrderBy(id => id));

        var windowsClaim = await _printService.ClaimPrintJob(new ClaimRestaurantPrintJobDto { AgentId = "windows-kitchen" });
        var androidClaim = await _printService.ClaimPrintJob(new ClaimRestaurantPrintJobDto { AgentId = "android-kitchen" });
        var secondAndroidClaim = await _printService.ClaimPrintJob(new ClaimRestaurantPrintJobDto { AgentId = "android-kitchen-2" });
        windowsClaim.Id.ShouldBe(jobId);
        androidClaim.Id.ShouldBe(jobId);
        secondAndroidClaim.Id.ShouldBe(jobId);
        windowsClaim.DeliveryId.ShouldNotBe(androidClaim.DeliveryId);
        windowsClaim.DeliveryId.ShouldNotBe(secondAndroidClaim.DeliveryId);
        androidClaim.DeliveryId.ShouldNotBe(secondAndroidClaim.DeliveryId);
        windowsClaim.LeaseToken.ShouldNotBeNull();
        androidClaim.LeaseToken.ShouldNotBeNull();
        secondAndroidClaim.LeaseToken.ShouldNotBeNull();
        (await _printService.ClaimPrintJob(new ClaimRestaurantPrintJobDto { AgentId = "android-kitchen" })).ShouldBeNull();
        (await _printService.ClaimPrintJob(new ClaimRestaurantPrintJobDto { AgentId = "android-kitchen-2" })).ShouldBeNull();
        (await _printService.ClaimPrintJob(new ClaimRestaurantPrintJobDto { AgentId = "windows-kitchen" })).ShouldBeNull();

        await UsingDbContextAsync(async context =>
        {
            var delivery = await context.RestaurantPrintDeliveries.SingleAsync(x => x.Id == androidClaim.DeliveryId);
            delivery.LeaseUntilUtc = DateTime.UtcNow.AddMinutes(-1);
        });
        var reclaimedAndroid = await _printService.ClaimPrintJob(new ClaimRestaurantPrintJobDto { AgentId = "android-kitchen" });
        reclaimedAndroid.DeliveryId.ShouldBe(androidClaim.DeliveryId);
        reclaimedAndroid.LeaseToken.ShouldNotBe(androidClaim.LeaseToken);

        await Should.ThrowAsync<UserFriendlyException>(() => _printService.ReportPrintJob(new ReportRestaurantPrintJobDto
        {
            Id = jobId,
            DeliveryId = androidClaim.DeliveryId,
            LeaseToken = androidClaim.LeaseToken,
            AgentId = "android-kitchen",
            Status = RestaurantPrintJobStatus.Printed
        }));

        await _printService.ReportPrintJob(new ReportRestaurantPrintJobDto
        {
            Id = jobId,
            DeliveryId = reclaimedAndroid.DeliveryId,
            LeaseToken = reclaimedAndroid.LeaseToken,
            AgentId = "android-kitchen",
            Status = RestaurantPrintJobStatus.Printed
        });
        await _printService.ReportPrintJob(new ReportRestaurantPrintJobDto
        {
            Id = jobId,
            DeliveryId = windowsClaim.DeliveryId,
            LeaseToken = windowsClaim.LeaseToken,
            AgentId = "windows-kitchen",
            Status = RestaurantPrintJobStatus.Printed
        });
        var completed = await _printService.ReportPrintJob(new ReportRestaurantPrintJobDto
        {
            Id = jobId,
            DeliveryId = secondAndroidClaim.DeliveryId,
            LeaseToken = secondAndroidClaim.LeaseToken,
            AgentId = "android-kitchen-2",
            Status = RestaurantPrintJobStatus.Printed
        });
        var allCopies = await UsingDbContextAsync(context => context.RestaurantPrintDeliveries
            .Where(delivery => delivery.TenantId == tenantId && delivery.PrintJobId == jobId)
            .ToListAsync());
        allCopies.Count.ShouldBe(3);
        allCopies.ShouldAllBe(delivery => delivery.Status == RestaurantPrintJobStatus.Printed);
        completed.Status.ShouldBe(RestaurantPrintJobStatus.Printed);
        (await _printService.ClaimPrintJob(new ClaimRestaurantPrintJobDto { AgentId = "android-kitchen" })).ShouldBeNull();
    }

    [Fact]
    public async Task Device_registration_does_not_backfill_jobs_and_disabled_device_gets_no_new_copy()
    {
        var tenantId = AbpSession.TenantId.Value;
        var oldJobId = Guid.NewGuid();
        await UsingDbContextAsync(async context =>
        {
            context.RestaurantPrintRoutes.Add(new RestaurantPrintRoute
            {
                TenantId = tenantId,
                Name = "receipt",
                DisplayName = "Receipt",
                IsActive = true
            });
            context.RestaurantPrintJobs.Add(new RestaurantPrintJob
            {
                Id = oldJobId,
                TenantId = tenantId,
                ExternalJobId = "old-receipt-1",
                Type = RestaurantPrintJobType.BillReceipt,
                RouteName = "receipt",
                Payload = new byte[] { 0x1b, 0x40 },
                Status = RestaurantPrintJobStatus.Pending
            });
        });

        var device = await _printService.RegisterPrintDevice(new RegisterRestaurantPrintDeviceDto
        {
            ClientDeviceId = "android-receipt",
            Name = "Reception phone",
            Platform = "Android",
            RouteNames = { "receipt" }
        });
        device.IsEnabled.ShouldBeTrue();
        var historicalDeliveries = await UsingDbContextAsync(context => context.RestaurantPrintDeliveries
            .Where(x => x.TenantId == tenantId && x.PrintJobId == oldJobId)
            .CountAsync());
        historicalDeliveries.ShouldBe(0);

        var newJobId = Guid.NewGuid();
        using (var unitOfWork = _unitOfWorkManager.Begin())
        {
            await _queueService.QueueAsync(new RestaurantPrintJob
            {
                Id = newJobId,
                TenantId = tenantId,
                ExternalJobId = "new-receipt-2",
                Type = RestaurantPrintJobType.BillReceipt,
                RouteName = "receipt",
                Payload = new byte[] { 0x1b, 0x40 }
            }, tenantId);
            await unitOfWork.CompleteAsync();
        }
        var newDeliveryCount = await UsingDbContextAsync(context => context.RestaurantPrintDeliveries
            .Where(x => x.TenantId == tenantId && x.PrintJobId == newJobId && x.DeviceId == device.Id)
            .CountAsync());
        newDeliveryCount.ShouldBe(1);

        await _printService.SetPrintDeviceEnabled(new SetRestaurantPrintDeviceEnabledDto { Id = device.Id, IsEnabled = false });
        var cancelledDelivery = await UsingDbContextAsync(context => context.RestaurantPrintDeliveries.SingleAsync(x =>
            x.TenantId == tenantId && x.PrintJobId == newJobId && x.DeviceId == device.Id));
        cancelledDelivery.Status.ShouldBe(RestaurantPrintJobStatus.Cancelled);
        var disabledJobId = Guid.NewGuid();
        using (var unitOfWork = _unitOfWorkManager.Begin())
        {
            await _queueService.QueueAsync(new RestaurantPrintJob
            {
                Id = disabledJobId,
                TenantId = tenantId,
                ExternalJobId = "receipt-after-disable",
                Type = RestaurantPrintJobType.BillReceipt,
                RouteName = "receipt",
                Payload = new byte[] { 0x1b, 0x40 }
            }, tenantId);
            await unitOfWork.CompleteAsync();
        }
        var disabledJob = await UsingDbContextAsync(context => context.RestaurantPrintJobs.SingleAsync(x => x.Id == disabledJobId));
        disabledJob.Status.ShouldBe(RestaurantPrintJobStatus.Failed);
        var disabledDeliveryCount = await UsingDbContextAsync(context => context.RestaurantPrintDeliveries
            .Where(x => x.TenantId == tenantId && x.PrintJobId == disabledJobId)
            .CountAsync());
        disabledDeliveryCount.ShouldBe(0);
    }
}
