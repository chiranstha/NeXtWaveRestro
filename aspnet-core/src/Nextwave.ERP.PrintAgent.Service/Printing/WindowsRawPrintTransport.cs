using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Nextwave.ERP.PrintAgent.Service.Printing;

public sealed class WindowsRawPrintTransport : IPrintTransport
{
    public bool CanHandle(PrinterRoute route) => route.Kind == PrinterRouteKind.WindowsQueue;

    public Task SendAsync(PrinterRoute route, byte[] payload, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!OpenPrinter(route.PrinterName!, out var printer, IntPtr.Zero))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        try
        {
            var document = new DocumentInfo { DocumentName = "Nextwave ERP POS Receipt", DataType = "RAW" };
            if (StartDocPrinter(printer, 1, document) == 0 || !StartPagePrinter(printer))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            try
            {
                if (!WritePrinter(printer, payload, payload.Length, out var written) || written != payload.Length)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }
            finally
            {
                EndPagePrinter(printer);
                EndDocPrinter(printer);
            }
        }
        finally
        {
            ClosePrinter(printer);
        }

        return Task.CompletedTask;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private sealed class DocumentInfo
    {
        [MarshalAs(UnmanagedType.LPWStr)] public string DocumentName = string.Empty;
        public string? OutputFile;
        [MarshalAs(UnmanagedType.LPWStr)] public string DataType = "RAW";
    }

    [DllImport("winspool.drv", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool OpenPrinter(string printerName, out IntPtr printer, IntPtr defaults);
    [DllImport("winspool.drv", SetLastError = true)] private static extern bool ClosePrinter(IntPtr printer);
    [DllImport("winspool.drv", SetLastError = true, CharSet = CharSet.Unicode)] private static extern int StartDocPrinter(IntPtr printer, int level, [In] DocumentInfo documentInfo);
    [DllImport("winspool.drv", SetLastError = true)] private static extern bool EndDocPrinter(IntPtr printer);
    [DllImport("winspool.drv", SetLastError = true)] private static extern bool StartPagePrinter(IntPtr printer);
    [DllImport("winspool.drv", SetLastError = true)] private static extern bool EndPagePrinter(IntPtr printer);
    [DllImport("winspool.drv", SetLastError = true)] private static extern bool WritePrinter(IntPtr printer, byte[] bytes, int count, out int written);
}
