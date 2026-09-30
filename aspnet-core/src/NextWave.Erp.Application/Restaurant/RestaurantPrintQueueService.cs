using Abp.Dependency;
using Abp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using NextWave.Erp.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NextWave.Erp.Restaurant
{
    public class RestaurantPrintQueueService(
        IRepository<RestaurantPrintJob, Guid> jobRepository,
        IRepository<RestaurantPrintRoute, Guid> routeRepository,
        IRepository<RestaurantPrintDevice, Guid> deviceRepository,
        IRepository<RestaurantPrintDeviceRoute, Guid> deviceRouteRepository,
        IRepository<RestaurantPrintDelivery, Guid> deliveryRepository) : ITransientDependency
    {
        public async Task QueueAsync(RestaurantPrintJob job, int? tenantId)
        {
            job.TenantId = tenantId;
            var routeName = job.RouteName?.Trim();
            var routeIsActive = !string.IsNullOrWhiteSpace(routeName) && routeName != "unconfigured" &&
                await routeRepository.GetAll().AnyAsync(x => x.TenantId == tenantId && x.IsActive && x.Name == routeName);

            var deviceIds = routeIsActive
                ? await (from mapping in deviceRouteRepository.GetAll()
                         join device in deviceRepository.GetAll() on mapping.DeviceId equals device.Id
                         where mapping.TenantId == tenantId && mapping.RouteName == routeName &&
                               device.TenantId == tenantId && device.IsEnabled
                         select device.Id).Distinct().ToListAsync()
                : new List<Guid>();

            if (deviceIds.Count == 0)
            {
                job.Status = RestaurantPrintJobStatus.Failed;
                job.LastError = routeIsActive
                    ? $"No enabled print device is configured for route '{routeName}'."
                    : "The print route is not configured or active.";
            }
            else
            {
                job.Status = RestaurantPrintJobStatus.Pending;
                job.LastError = null;
            }

            var jobId = await jobRepository.InsertAndGetIdAsync(job);
            foreach (var deviceId in deviceIds)
            {
                await deliveryRepository.InsertAsync(new RestaurantPrintDelivery
                {
                    TenantId = tenantId,
                    PrintJobId = jobId,
                    DeviceId = deviceId,
                    RouteName = routeName,
                    Status = RestaurantPrintJobStatus.Pending,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
        }
    }
}
