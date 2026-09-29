using System.Text.Json;
using Microsoft.Extensions.Options;
using Nextwave.ERP.PrintAgent.Service.Configuration;

namespace Nextwave.ERP.PrintAgent.Service.Printing;

public sealed class RouteStore
{
    private readonly string _path;
    private readonly object _sync = new();
    private List<PrinterRoute> _routes;

    public RouteStore(IOptions<PrintAgentOptions> options)
    {
        _path = Path.Combine(options.Value.GetDataDirectory(), "routes.json");
        _routes = Load();
    }

    public IReadOnlyList<PrinterRoute> GetAll()
    {
        lock (_sync)
        {
            return _routes.ToArray();
        }
    }

    public PrinterRoute? Find(string name)
    {
        lock (_sync)
        {
            return _routes.FirstOrDefault(route => string.Equals(route.Name, name, StringComparison.OrdinalIgnoreCase));
        }
    }

    public void Replace(IEnumerable<PrinterRoute> routes)
    {
        var validated = routes.Select(Validate).ToList();
        if (validated.Select(route => route.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != validated.Count)
        {
            throw new InvalidDataException("Route names must be unique.");
        }

        lock (_sync)
        {
            _routes = validated;
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, JsonSerializer.Serialize(_routes, new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    private List<PrinterRoute> Load()
    {
        try
        {
            if (File.Exists(_path))
            {
                return JsonSerializer.Deserialize<List<PrinterRoute>>(File.ReadAllText(_path)) ?? [];
            }

            var defaultPrinter = WindowsPrinterDiscovery.GetDefaultPrinterName()
                                  ?? WindowsPrinterDiscovery.GetPrinterNames().FirstOrDefault();
            return string.IsNullOrWhiteSpace(defaultPrinter)
                ? []
                : [new PrinterRoute("nextwavepos", PrinterRouteKind.WindowsQueue, PrinterName: defaultPrinter)];
        }
        catch (Exception)
        {
            return [];
        }
    }

    private static PrinterRoute Validate(PrinterRoute route)
    {
        if (string.IsNullOrWhiteSpace(route.Name) || route.Name.Length > 128 ||
            route.Name.Any(character => !char.IsLetterOrDigit(character) && character is not '-' and not '_'))
        {
            throw new InvalidDataException("Route names may contain only letters, numbers, hyphens, and underscores.");
        }

        if (route.Kind == PrinterRouteKind.WindowsQueue && string.IsNullOrWhiteSpace(route.PrinterName))
        {
            throw new InvalidDataException("A Windows printer name is required.");
        }

        if (route.Kind == PrinterRouteKind.Tcp9100 &&
            (string.IsNullOrWhiteSpace(route.Host) || route.Port is < 1 or > 65535))
        {
            throw new InvalidDataException("A valid TCP host and port are required.");
        }

        return route with { Name = route.Name.Trim(), PrinterName = route.PrinterName?.Trim(), Host = route.Host?.Trim() };
    }
}
