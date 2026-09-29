namespace Nextwave.ERP.PrintAgent.Service.Configuration;

public sealed class PrintAgentOptions
{
    public string Urls { get; set; } = "https://localhost:631";
    public string[] AllowedOrigins { get; set; } = [];
    public string DataDirectory { get; set; } = "%ProgramData%\\NextwaveERP\\PrintAgent";
    public int JobLifetimeHours { get; set; } = 24;
    public string ManagementPipeName { get; set; } = "NextwaveERP.PrintAgent.Management";

    public string GetDataDirectory()
    {
        return Environment.ExpandEnvironmentVariables(DataDirectory);
    }
}
