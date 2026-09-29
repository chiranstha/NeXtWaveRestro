using System.Text;
using System.Text.Json;
using Nextwave.ERP.PrintAgent;

namespace Nextwave.ERP.PrintAgent.Tray;

internal sealed class RouteSettingsForm : Form
{
    private readonly TextBox _alias = new() { Text = "nextwavepos", Dock = DockStyle.Fill };
    private readonly ComboBox _kind = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly ComboBox _printer = new() { DropDownStyle = ComboBoxStyle.DropDown, Dock = DockStyle.Fill };
    private readonly TextBox _host = new() { Dock = DockStyle.Fill };
    private readonly NumericUpDown _port = new() { Minimum = 1, Maximum = 65535, Value = 9100, Dock = DockStyle.Fill };
    private readonly Button _save = new() { Text = "Save route", Dock = DockStyle.Fill };

    public RouteSettingsForm()
    {
        Text = "Nextwave ERP Printer Route";
        Width = 520;
        Height = 310;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        _kind.Items.AddRange(["Windows printer queue", "TCP 9100 printer"]);
        _kind.SelectedIndex = 0;
        _kind.SelectedIndexChanged += (_, _) => UpdateFields();
        _save.Click += async (_, _) => await SaveAsync();

        var grid = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 2, RowCount = 6 };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        AddRow(grid, 0, "Route alias", _alias);
        AddRow(grid, 1, "Connection", _kind);
        AddRow(grid, 2, "Windows printer", _printer);
        AddRow(grid, 3, "TCP host", _host);
        AddRow(grid, 4, "TCP port", _port);
        grid.Controls.Add(_save, 1, 5);
        Controls.Add(grid);
        Shown += async (_, _) => await LoadAsync();
        UpdateFields();
    }

    private async Task LoadAsync()
    {
        var printersJson = await PipeClient.SendAsync("GET_PRINTERS");
        if (printersJson is not null)
        {
            _printer.Items.AddRange((JsonSerializer.Deserialize<string[]>(printersJson) ?? []).Cast<object>().ToArray());
        }

        var routesJson = await PipeClient.SendAsync("GET_ROUTES");
        var route = routesJson is null ? null : JsonSerializer.Deserialize<PrinterRoute[]>(routesJson)?.FirstOrDefault();
        if (route is null) return;
        _alias.Text = route.Name;
        _kind.SelectedIndex = route.Kind == PrinterRouteKind.WindowsQueue ? 0 : 1;
        _printer.Text = route.PrinterName ?? string.Empty;
        _host.Text = route.Host ?? string.Empty;
        _port.Value = Math.Clamp(route.Port, 1, 65535);
    }

    private async Task SaveAsync()
    {
        var route = _kind.SelectedIndex == 0
            ? new PrinterRoute(_alias.Text, PrinterRouteKind.WindowsQueue, PrinterName: _printer.Text)
            : new PrinterRoute(_alias.Text, PrinterRouteKind.Tcp9100, Host: _host.Text, Port: (int)_port.Value);
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new[] { route })));
        var result = await PipeClient.SendAsync("SET_ROUTES:" + encoded);
        if (result == "SAVED")
        {
            MessageBox.Show("Printer route saved.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
        }
        else
        {
            MessageBox.Show("The route could not be saved. Check the service and values.", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void UpdateFields()
    {
        _printer.Enabled = _kind.SelectedIndex == 0;
        _host.Enabled = _kind.SelectedIndex == 1;
        _port.Enabled = _kind.SelectedIndex == 1;
    }

    private static void AddRow(TableLayoutPanel grid, int row, string label, Control control)
    {
        grid.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
        grid.Controls.Add(control, 1, row);
    }
}
