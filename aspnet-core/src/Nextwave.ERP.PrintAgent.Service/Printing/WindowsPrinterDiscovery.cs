using System.Runtime.InteropServices;
using System.Text;

namespace Nextwave.ERP.PrintAgent.Service.Printing;

public static class WindowsPrinterDiscovery
{
    public static string? GetDefaultPrinterName()
    {
        var capacity = 512;
        var buffer = new StringBuilder(capacity);
        return GetDefaultPrinter(buffer, ref capacity) ? buffer.ToString() : null;
    }

    public static IReadOnlyList<string> GetPrinterNames()
    {
        EnumPrinters(PrinterEnumLocal | PrinterEnumConnections, null, 4, IntPtr.Zero, 0, out var required, out _);
        if (required == 0)
        {
            return [];
        }

        var buffer = Marshal.AllocHGlobal((int)required);
        try
        {
            if (!EnumPrinters(PrinterEnumLocal | PrinterEnumConnections, null, 4, buffer, required, out _, out var count))
            {
                return [];
            }

            var size = Marshal.SizeOf<PrinterInfo4>();
            var printers = new List<string>((int)count);
            for (var index = 0; index < count; index++)
            {
                var printer = Marshal.PtrToStructure<PrinterInfo4>(buffer + index * size);
                if (!string.IsNullOrWhiteSpace(printer.PrinterName))
                {
                    printers.Add(printer.PrinterName);
                }
            }
            return printers.Order(StringComparer.CurrentCultureIgnoreCase).ToArray();
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private const int PrinterEnumLocal = 0x00000002;
    private const int PrinterEnumConnections = 0x00000004;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct PrinterInfo4
    {
        public string PrinterName;
        public string ServerName;
        public uint Attributes;
    }

    [DllImport("winspool.drv", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool EnumPrinters(int flags, string? name, int level, IntPtr printerEnum,
        uint bufferSize, out uint required, out uint returned);

    [DllImport("winspool.drv", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool GetDefaultPrinter(StringBuilder printerName, ref int needed);
}
