namespace Abp.AspNetZeroCore.Licensing
{
    public class LicenseCheckInfo
    {
        public string UniqueComputerId { get; set; } = string.Empty;

        public string ProjectAssemblyName { get; set; } = string.Empty;

        public string LicenseController { get; set; } = string.Empty;

        public string ComputerName { get; set; } = string.Empty;

        public string ControlCode { get; set; } = string.Empty;

        public DateTime DateOfClient { get; set; }
    }
}