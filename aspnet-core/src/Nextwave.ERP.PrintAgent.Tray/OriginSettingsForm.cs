using System.Text;
using System.Text.Json;

namespace Nextwave.ERP.PrintAgent.Tray;

internal sealed class OriginSettingsForm : Form
{
    private readonly TextBox _origins = new()
    {
        Dock = DockStyle.Fill,
        Multiline = true,
        ScrollBars = ScrollBars.Vertical,
        AcceptsReturn = true,
        AcceptsTab = false
    };

    private readonly Button _save = new() { Text = "Save origins", Dock = DockStyle.Fill };

    public OriginSettingsForm()
    {
        Text = "Nextwave ERP Allowed Origins";
        Width = 620;
        Height = 360;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var instructions = new Label
        {
            Text = "Enter one ERP origin per line, for example https://erp.example.com or http://localhost:4200.",
            Dock = DockStyle.Fill,
            AutoSize = false
        };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 1,
            RowCount = 3
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.Controls.Add(instructions, 0, 0);
        layout.Controls.Add(_origins, 0, 1);
        layout.Controls.Add(_save, 0, 2);
        Controls.Add(layout);

        _save.Click += async (_, _) => await SaveAsync();
        Shown += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var json = await PipeClient.SendAsync("GET_ORIGINS");
        if (json is null)
        {
            MessageBox.Show("The print service is not available.", Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        _origins.Text = string.Join(Environment.NewLine,
            JsonSerializer.Deserialize<string[]>(json) ?? []);
    }

    private async Task SaveAsync()
    {
        var values = _origins.Lines
            .Select(origin => origin.Trim())
            .Where(origin => origin.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(values)));
        var result = await PipeClient.SendAsync("SET_ORIGINS:" + encoded);
        if (result == "SAVED")
        {
            MessageBox.Show("ERP origins saved.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
            return;
        }

        MessageBox.Show("The origins could not be saved. Check that each value is an HTTP or HTTPS origin without a path.",
            Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
