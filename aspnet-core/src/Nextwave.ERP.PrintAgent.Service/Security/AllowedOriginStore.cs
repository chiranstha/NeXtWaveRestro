using System.Text.Json;
using Microsoft.Extensions.Options;
using Nextwave.ERP.PrintAgent.Service.Configuration;

namespace Nextwave.ERP.PrintAgent.Service.Security;

public sealed class AllowedOriginStore
{
    private readonly string _path;
    private readonly object _sync = new();
    private string[] _origins;

    public AllowedOriginStore(IOptions<PrintAgentOptions> options)
        : this(options.Value)
    {
    }

    public AllowedOriginStore(PrintAgentOptions options)
    {
        _path = Path.Combine(options.GetDataDirectory(), "origins.json");
        _origins = Load(options.AllowedOrigins);
    }

    public IReadOnlyList<string> GetAll()
    {
        lock (_sync)
        {
            return _origins.ToArray();
        }
    }

    public bool IsAllowed(string? origin)
    {
        if (string.IsNullOrWhiteSpace(origin))
        {
            return false;
        }

        string normalized;
        try
        {
            normalized = Normalize(origin);
        }
        catch (InvalidDataException)
        {
            return false;
        }
        lock (_sync)
        {
            return _origins.Contains(normalized, StringComparer.OrdinalIgnoreCase);
        }
    }

    public void Replace(IEnumerable<string> origins)
    {
        var normalized = origins
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Select(Normalize)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(origin => origin, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        lock (_sync)
        {
            _origins = normalized;
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, JsonSerializer.Serialize(_origins, new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    private string[] Load(IEnumerable<string> defaults)
    {
        try
        {
            if (File.Exists(_path))
            {
                var stored = JsonSerializer.Deserialize<string[]>(File.ReadAllText(_path));
                if (stored is not null)
                {
                    return NormalizeAll(stored);
                }
            }
        }
        catch (Exception)
        {
            // Fall back to the configured defaults when the local override is invalid.
        }

        return NormalizeAll(defaults);
    }

    private static string[] NormalizeAll(IEnumerable<string> origins)
    {
        return origins
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Select(Normalize)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(origin => origin, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string Normalize(string origin)
    {
        if (!Uri.TryCreate(origin.Trim(), UriKind.Absolute, out var uri) ||
            (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
             !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)) ||
            !string.IsNullOrEmpty(uri.AbsolutePath.Trim('/')) ||
            !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment))
        {
            throw new InvalidDataException("ERP origins must be absolute HTTP or HTTPS origins without a path.");
        }

        return uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
    }
}
