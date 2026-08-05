using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace NextWave.Erp.Web.Authentication.JwtBearer;

public class AsyncJwtBearerOptions : JwtBearerOptions
{
    public readonly List<IAsyncSecurityTokenValidator> AsyncSecurityTokenValidators;

    private readonly ErpAsyncJwtSecurityTokenHandler _defaultAsyncHandler = new ErpAsyncJwtSecurityTokenHandler();

    public AsyncJwtBearerOptions()
    {
        AsyncSecurityTokenValidators = new List<IAsyncSecurityTokenValidator>() { _defaultAsyncHandler };
    }
}


