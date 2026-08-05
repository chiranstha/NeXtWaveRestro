using System.Collections.Generic;

namespace NextWave.Erp.Authorization.Users.Profile.Dto;

public class UpdateGoogleAuthenticatorKeyInput
{
    public string GoogleAuthenticatorKey { get; set; }
    public string AuthenticatorCode { get; set; }
}

