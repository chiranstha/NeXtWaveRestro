using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using Nextwave.ERP.PrintAgent.Service.Configuration;

namespace Nextwave.ERP.PrintAgent.Service.Security;

public sealed class AgentSecurityStore
{
    private readonly IDataProtector _protector;
    private readonly string _path;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private SecurityData _data = new();

    public AgentSecurityStore(IDataProtectionProvider provider, IOptions<PrintAgentOptions> options)
    {
        _protector = provider.CreateProtector("Nextwave.ERP.PrintAgent.Security.v1");
        _path = Path.Combine(options.Value.GetDataDirectory(), "security.dat");
        Load();
    }

    public bool IsPaired => _data.Tokens.Count > 0;

    public string CreatePairingCode()
    {
        _data.PairingCode = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        _data.PairingCodeExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5);
        Save();
        return _data.PairingCode;
    }

    public async Task<string?> PairAsync(string code, string origin)
    {
        await _gate.WaitAsync();
        try
        {
            if (_data.PairingCodeExpiresAt < DateTimeOffset.UtcNow ||
                !FixedEquals(_data.PairingCode, code))
            {
                return null;
            }

            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            _data.Tokens.RemoveAll(item => string.Equals(item.Origin, NormalizeOrigin(origin), StringComparison.OrdinalIgnoreCase));
            _data.Tokens.Add(new TokenData(Hash(token), NormalizeOrigin(origin)));
            _data.PairingCode = null;
            _data.PairingCodeExpiresAt = null;
            Save();
            return token;
        }
        finally
        {
            _gate.Release();
        }
    }

    public bool Validate(string? token, string? origin)
    {
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(origin))
        {
            return false;
        }

        var tokenHash = Hash(token);
        var normalizedOrigin = NormalizeOrigin(origin);
        return _data.Tokens.Any(item =>
            FixedEquals(item.TokenHash, tokenHash) &&
            string.Equals(item.Origin, normalizedOrigin, StringComparison.OrdinalIgnoreCase));
    }

    public void RevokeAll()
    {
        _data.Tokens.Clear();
        Save();
    }

    private void Load()
    {
        try
        {
            if (File.Exists(_path))
            {
                _data = JsonSerializer.Deserialize<SecurityData>(_protector.Unprotect(File.ReadAllText(_path))) ?? new();
            }
        }
        catch (Exception)
        {
            _data = new SecurityData();
        }
    }

    private void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var temporaryPath = _path + ".tmp";
        File.WriteAllText(temporaryPath, _protector.Protect(JsonSerializer.Serialize(_data)));
        File.Move(temporaryPath, _path, true);
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static bool FixedEquals(string? left, string? right)
    {
        if (left is null || right is null)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(left), Encoding.UTF8.GetBytes(right));
    }

    private static string NormalizeOrigin(string origin) => new Uri(origin).GetLeftPart(UriPartial.Authority).TrimEnd('/');

    private sealed class SecurityData
    {
        public string? PairingCode { get; set; }
        public DateTimeOffset? PairingCodeExpiresAt { get; set; }
        public List<TokenData> Tokens { get; set; } = [];
    }

    private sealed record TokenData(string TokenHash, string Origin);
}
