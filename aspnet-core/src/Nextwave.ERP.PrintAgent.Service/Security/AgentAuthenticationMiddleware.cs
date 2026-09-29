namespace Nextwave.ERP.PrintAgent.Service.Security;

public sealed class AgentAuthenticationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, AgentSecurityStore securityStore)
    {
        if (context.Request.Path.Equals("/api/v1/health") ||
            context.Request.Path.Equals("/api/v1/pair") ||
            HttpMethods.IsOptions(context.Request.Method))
        {
            await next(context);
            return;
        }

        var origin = context.Request.Headers.Origin.FirstOrDefault();
        var authorization = context.Request.Headers.Authorization.FirstOrDefault();
        var token = authorization?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true
            ? authorization[7..]
            : null;

        if (!securityStore.Validate(token, origin))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await next(context);
    }
}
