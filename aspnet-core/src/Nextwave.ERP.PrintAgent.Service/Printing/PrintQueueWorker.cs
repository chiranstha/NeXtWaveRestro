namespace Nextwave.ERP.PrintAgent.Service.Printing;

public sealed class PrintQueueWorker(
    PrintQueue queue,
    RouteStore routeStore,
    IEnumerable<IPrintTransport> transports,
    ILogger<PrintQueueWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                foreach (var job in await queue.GetDueJobsAsync())
                {
                    await ProcessAsync(job, stoppingToken);
                }

                await queue.WaitForWorkAsync(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Host shutdown/restart cancellation is expected and is not a print failure.
        }
    }

    private async Task ProcessAsync(PrintJobRecord job, CancellationToken cancellationToken)
    {
        var route = routeStore.Find(job.Route);
        var transport = route is null ? null : transports.FirstOrDefault(item => item.CanHandle(route));
        if (route is null || transport is null)
        {
            await queue.UpdateAsync(job, PrintJobState.Failed, "The configured printer route is unavailable.");
            return;
        }

        try
        {
            await queue.UpdateAsync(job, PrintJobState.Printing);
            await transport.SendAsync(route, job.Payload, cancellationToken);
            await queue.UpdateAsync(job, PrintJobState.Printed);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Print job {PrintJobId} failed on route {Route}", job.Id, job.Route);
            await queue.UpdateAsync(job, PrintJobState.Failed, exception.Message);
        }
    }
}
