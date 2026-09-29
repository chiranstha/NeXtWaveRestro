using System;

namespace NextWave.Erp;

/// <summary>
/// Some consts used in the application.
/// </summary>
public class AppConsts
{
    /// <summary>
    /// Default page size for paged requests.
    /// </summary>
    public const int DefaultPageSize = 10;

    /// <summary>
    /// Maximum allowed page size for paged requests.
    /// </summary>
    public const int MaxPageSize = 1000;

    /// <summary>
    /// Default pass phrase for SimpleStringCipher decrypt/encrypt operations
    /// </summary>
    public const string DefaultPassPhrase = "gsKxGZ012HLL3MI5";

    public const string TokenValidityKey = "token_validity_key";
    public const string RefreshTokenValidityKey = "refresh_token_validity_key";
    public const string SecurityStampKey = "AspNet.Identity.SecurityStamp";

    public const string TokenType = "token_type";

    public static string UserIdentifier = "user_identifier";

    public const string ThemeDefault = "default";
    public const string Theme2 = "theme2";
    public const string Theme3 = "theme3";
    public const string Theme8 = "theme8";
    public const string Theme11 = "theme11";

    public static string NormalizeTheme(string? theme) => theme?.Trim().ToLowerInvariant() switch
    {
        Theme2 => Theme2,
        Theme3 => Theme3,
        Theme8 => Theme8,
        Theme11 => Theme11,
        _ => ThemeDefault
    };

    public static TimeSpan AccessTokenExpiration = TimeSpan.FromDays(1);
    public static TimeSpan RefreshTokenExpiration = TimeSpan.FromDays(365);
}

