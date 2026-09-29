using System.IO.Pipes;
using System.Text;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.Json;
using Nextwave.ERP.PrintAgent.Service.Security;
using Nextwave.ERP.PrintAgent.Service.Printing;
using Nextwave.ERP.PrintAgent.Service.Configuration;
using Microsoft.Extensions.Options;

namespace Nextwave.ERP.PrintAgent.Service.Management;

public sealed class NamedPipeManagementWorker(
    AgentSecurityStore securityStore,
    RouteStore routeStore,
    AllowedOriginStore originStore,
    IOptions<PrintAgentOptions> options) : BackgroundService
{
    private readonly string _pipeName = options.Value.ManagementPipeName;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await HandleConnectionAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception)
            {
                // A broken tray connection must not disable future management sessions.
            }
        }
    }

    private async Task HandleConnectionAsync(CancellationToken stoppingToken)
    {
        var pipeSecurity = new PipeSecurity();
        pipeSecurity.AddAccessRule(new PipeAccessRule(
            new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null),
            PipeAccessRights.ReadWrite,
            AccessControlType.Allow));
        pipeSecurity.AddAccessRule(new PipeAccessRule(
            new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
            PipeAccessRights.FullControl,
            AccessControlType.Allow));
        await using var pipe = NamedPipeServerStreamAcl.Create(
            _pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous,
            0, 0, pipeSecurity);
        await pipe.WaitForConnectionAsync(stoppingToken);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));
        var buffer = new byte[64 * 1024];
        var read = await pipe.ReadAsync(buffer, timeout.Token);
        if (read == 0) return;
        var command = Encoding.UTF8.GetString(buffer, 0, read).Trim('\uFEFF', '\r', '\n', ' ');
        var response = HandleCommand(command);
        var responseBytes = Encoding.UTF8.GetBytes(response + "\n");
        await pipe.WriteAsync(responseBytes, timeout.Token);
        await pipe.FlushAsync(timeout.Token);
    }

    private string HandleCommand(string? command)
    {
        if (command == "PAIR_CODE") return securityStore.CreatePairingCode();
        if (command == "REVOKE") return Revoke();
        if (command == "GET_ROUTES") return JsonSerializer.Serialize(routeStore.GetAll());
        if (command == "GET_PRINTERS") return JsonSerializer.Serialize(WindowsPrinterDiscovery.GetPrinterNames());
        if (command == "GET_ORIGINS") return JsonSerializer.Serialize(originStore.GetAll());
        if (command?.StartsWith("SET_ROUTES:", StringComparison.Ordinal) == true)
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(command[11..]));
            routeStore.Replace(JsonSerializer.Deserialize<List<PrinterRoute>>(json) ?? []);
            return "SAVED";
        }
        if (command?.StartsWith("SET_ORIGINS:", StringComparison.Ordinal) == true)
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(command[12..]));
            originStore.Replace(JsonSerializer.Deserialize<List<string>>(json) ?? []);
            return "SAVED";
        }
        return "UNKNOWN_COMMAND";
    }

    private string Revoke()
    {
        securityStore.RevokeAll();
        return "REVOKED";
    }
}
