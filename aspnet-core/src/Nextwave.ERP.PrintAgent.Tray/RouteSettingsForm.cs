using System.Text;
using System.Text.Json;
using Nextwave.ERP.PrintAgent;

namespace Nextwave.ERP.PrintAgent.Tray;

internal sealed class RouteSettingsForm : Form
{
    private readonly ListBox _routeList = new() { Dock = DockStyle.Fill };
    private readonly TextBox _alias = new() { Dock = DockStyle.Fill };
    private readonly ComboBox _kind = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly ComboBox _printer = new() { DropDownStyle = ComboBoxStyle.DropDown, Dock = DockStyle.Fill };
    private readonly TextBox _host = new() { Dock = DockStyle.Fill };
    private readonly NumericUpDown _port = new() { Minimum = 1, Maximum = 65535, Value = 9100, Dock = DockStyle.Fill };
    private readonly Button _save = new() { Text = "Save routes", AutoSize = true };
    private readonly Button _add = new() { Text = "Add route", AutoSize = true };
    private readonly Button _remove = new() { Text = "Remove route", AutoSize = true };
    private readonly List<PrinterRoute> _routes = [];
    private bool _updatingSelection;
    private int _selectedIndex = -1;

    public RouteSettingsForm()
    {
        Text = "Nextwave ERP Printer Routes";
        Width = 760;
        Height = 430;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        _kind.Items.AddRange(["Windows printer queue", "TCP 9100 printer"]);
        _kind.SelectedIndex = 0;
        _kind.SelectedIndexChanged += (_, _) => UpdateFields();
        _routeList.SelectedIndexChanged += (_, _) => SelectRoute(_routeList.SelectedIndex);
        _add.Click += (_, _) => AddRoute();
        _remove.Click += (_, _) => RemoveRoute();
        _save.Click += async (_, _) => await SaveAsync();

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 3, RowCount = 6 };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 125));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < 5; i++) root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(new Label { Text = "Routes", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        root.Controls.Add(_routeList, 0, 1);
        root.SetRowSpan(_routeList, 4);
        AddRow(root, 0, "Route alias", _alias, 1);
        AddRow(root, 1, "Connection", _kind, 1);
        AddRow(root, 2, "Windows printer", _printer, 1);
        AddRow(root, 3, "TCP host", _host, 1);
        AddRow(root, 4, "TCP port", _port, 1);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, AutoSize = true };
        buttons.Controls.Add(_save);
        buttons.Controls.Add(_remove);
        buttons.Controls.Add(_add);
        root.Controls.Add(buttons, 0, 5);
        root.SetColumnSpan(buttons, 3);
        Controls.Add(root);
        Shown += async (_, _) => await LoadAsync();
        UpdateFields();
    }

    private async Task LoadAsync()
    {
        var printersJson = await PipeClient.SendAsync("GET_PRINTERS");
        if (printersJson is not null)
            _printer.Items.AddRange((JsonSerializer.Deserialize<string[]>(printersJson) ?? []).Cast<object>().ToArray());

        var routesJson = await PipeClient.SendAsync("GET_ROUTES");
        if (routesJson is not null) _routes.AddRange(JsonSerializer.Deserialize<PrinterRoute[]>(routesJson) ?? []);
        RefreshRouteList(_routes.Count == 0 ? -1 : 0);
    }

    private void SelectRoute(int index)
    {
        if (_updatingSelection) return;
        CommitSelectedRoute();
        _selectedIndex = index;
        if (index < 0 || index >= _routes.Count)
        {
            _alias.Clear();
            _printer.Text = string.Empty;
            _host.Clear();
            return;
        }

        var route = _routes[index];
        _alias.Text = route.Name;
        _kind.SelectedIndex = route.Kind == PrinterRouteKind.WindowsQueue ? 0 : 1;
        _printer.Text = route.PrinterName ?? string.Empty;
        _host.Text = route.Host ?? string.Empty;
        _port.Value = Math.Clamp(route.Port, 1, 65535);
        UpdateFields();
    }

    private void AddRoute()
    {
        CommitSelectedRoute();
        var alias = "route" + (_routes.Count + 1);
        var suffix = 1;
        while (_routes.Any(route => string.Equals(route.Name, alias, StringComparison.OrdinalIgnoreCase)))
            alias = "route" + (_routes.Count + ++suffix);
        _routes.Add(new PrinterRoute(alias, PrinterRouteKind.WindowsQueue));
        RefreshRouteList(_routes.Count - 1);
    }

    private void RemoveRoute()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _routes.Count) return;
        _routes.RemoveAt(_selectedIndex);
        RefreshRouteList(_routes.Count == 0 ? -1 : Math.Min(_selectedIndex, _routes.Count - 1));
    }

    private async Task SaveAsync()
    {
        CommitSelectedRoute();
        if (_routes.Count == 0)
        {
            MessageBox.Show("Add at least one route before saving.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (_routes.Any(route => string.IsNullOrWhiteSpace(route.Name)) ||
            _routes.Select(route => route.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != _routes.Count)
        {
            MessageBox.Show("Every route needs a unique alias.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(_routes)));
        var result = await PipeClient.SendAsync("SET_ROUTES:" + encoded);
        if (result == "SAVED")
        {
            MessageBox.Show("Printer routes saved.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
        }
        else
        {
            MessageBox.Show("The routes could not be saved. Check the service and route values.", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void CommitSelectedRoute()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _routes.Count) return;
        var alias = _alias.Text.Trim();
        var route = _kind.SelectedIndex == 0
            ? new PrinterRoute(alias, PrinterRouteKind.WindowsQueue, PrinterName: _printer.Text.Trim())
            : new PrinterRoute(alias, PrinterRouteKind.Tcp9100, Host: _host.Text.Trim(), Port: (int)_port.Value);
        _routes[_selectedIndex] = route;
        if (_selectedIndex < _routeList.Items.Count) _routeList.Items[_selectedIndex] = alias;
    }

    private void RefreshRouteList(int selectIndex)
    {
        _updatingSelection = true;
        _routeList.Items.Clear();
        foreach (var route in _routes) _routeList.Items.Add(route.Name);
        _selectedIndex = -1;
        _routeList.SelectedIndex = selectIndex;
        _updatingSelection = false;
        if (selectIndex >= 0) SelectRoute(selectIndex);
        else SelectRoute(-1);
    }

    private void UpdateFields()
    {
        _printer.Enabled = _kind.SelectedIndex == 0;
        _host.Enabled = _kind.SelectedIndex == 1;
        _port.Enabled = _kind.SelectedIndex == 1;
    }

    private static void AddRow(TableLayoutPanel grid, int row, string label, Control control, int column)
    {
        grid.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, column, row);
        grid.Controls.Add(control, column + 1, row);
    }
}
