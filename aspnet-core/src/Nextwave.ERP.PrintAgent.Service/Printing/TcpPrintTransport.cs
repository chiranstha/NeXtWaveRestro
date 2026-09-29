using System.Net.Sockets;

namespace Nextwave.ERP.PrintAgent.Service.Printing;

public sealed class TcpPrintTransport : IPrintTransport
{
    public bool CanHandle(PrinterRoute route) => route.Kind == PrinterRouteKind.Tcp9100;

    public async Task SendAsync(PrinterRoute route, byte[] payload, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        using var client = new TcpClient();
        await client.ConnectAsync(route.Host!, route.Port, timeout.Token);
        await client.GetStream().WriteAsync(payload, timeout.Token);
        await client.GetStream().FlushAsync(timeout.Token);
    }
}
