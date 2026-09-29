using System.IO.Pipes;
using System.Text;

namespace Nextwave.ERP.PrintAgent.Tray;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new PrintAgentContext());
    }
}

internal sealed class PrintAgentContext : ApplicationContext
{
    private readonly NotifyIcon _notifyIcon;

    public PrintAgentContext()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Show pairing code", null, async (_, _) => await ShowPairingCodeAsync());
        menu.Items.Add("Manage printer routes", null, (_, _) => new RouteSettingsForm().ShowDialog());
        menu.Items.Add("Configure ERP origins", null, (_, _) => new OriginSettingsForm().ShowDialog());
        menu.Items.Add("Open agent status", null, (_, _) => OpenStatus());
        menu.Items.Add("Revoke browser access", null, async (_, _) => await RevokeAsync());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit tray", null, (_, _) => ExitThread());

        var trayIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;
        _notifyIcon = new NotifyIcon
        {
            Text = "Nextwave ERP Print Agent",
            Icon = trayIcon,
            ContextMenuStrip = menu,
            Visible = true
        };
        _notifyIcon.DoubleClick += (_, _) => OpenStatus();
    }

    protected override void ExitThreadCore()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        base.ExitThreadCore();
    }

    private static async Task ShowPairingCodeAsync()
    {
        var code = await PipeClient.SendAsync("PAIR_CODE");
        MessageBox.Show(code is null ? "The print service is not available." : $"Pairing code: {code}\n\nThis code expires in 5 minutes.",
            "Nextwave ERP Print Agent", MessageBoxButtons.OK, code is null ? MessageBoxIcon.Error : MessageBoxIcon.Information);
    }

    private static async Task RevokeAsync()
    {
        if (MessageBox.Show("Revoke access for every paired browser?", "Nextwave ERP Print Agent",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
        {
            await PipeClient.SendAsync("REVOKE");
        }
    }

    private static void OpenStatus()
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://localhost:631/api/v1/health")
        {
            UseShellExecute = true
        });
    }

}

internal static class PipeClient
{
    public static async Task<string?> SendAsync(string command)
    {
        try
        {
            await using var pipe = new NamedPipeClientStream(".", "NextwaveERP.PrintAgent.Management", PipeDirection.InOut, PipeOptions.Asynchronous);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await pipe.ConnectAsync(timeout.Token);
            await pipe.WriteAsync(Encoding.UTF8.GetBytes(command + "\n"), timeout.Token);
            await pipe.FlushAsync(timeout.Token);
            var buffer = new byte[64 * 1024];
            var read = await pipe.ReadAsync(buffer, timeout.Token);
            return read == 0 ? null : Encoding.UTF8.GetString(buffer, 0, read).Trim();
        }
        catch (Exception)
        {
            return null;
        }
    }
}
