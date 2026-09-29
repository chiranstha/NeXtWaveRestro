namespace Nextwave.ERP.PrintAgent.Service.Printing;

public interface IPrintTransport
{
    bool CanHandle(PrinterRoute route);
    Task SendAsync(PrinterRoute route, byte[] payload, CancellationToken cancellationToken);
}
