using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;
using Nextwave.ERP.PrintAgent;
using Nextwave.ERP.PrintAgent.Ipp;
using Nextwave.ERP.PrintAgent.Service.Configuration;
using Nextwave.ERP.PrintAgent.Service.Management;
using Nextwave.ERP.PrintAgent.Service.Printing;
using Nextwave.ERP.PrintAgent.Service.Security;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseWindowsService(options => options.ServiceName = "Nextwave ERP Print Agent");
builder.Services.Configure<PrintAgentOptions>(builder.Configuration.GetSection("PrintAgent"));
var options = builder.Configuration.GetSection("PrintAgent").Get<PrintAgentOptions>() ?? new PrintAgentOptions();
var allowedOriginStore = new AllowedOriginStore(options);
var dataDirectory = options.GetDataDirectory();
Directory.CreateDirectory(dataDirectory);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.File(Path.Combine(dataDirectory, "logs", "agent-.log"), rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14)
    .CreateLogger();
builder.Host.UseSerilog();

builder.WebHost.ConfigureKestrel(kestrel =>
{
    kestrel.Limits.MaxRequestBodySize = IppRequestParser.MaxDocumentBytes + 16 * 1024;
    var listenUri = new Uri(options.Urls);
    kestrel.ListenLocalhost(listenUri.Port, listen =>
    {
        listen.Protocols = HttpProtocols.Http1AndHttp2;
        if (listenUri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            listen.UseHttps(CertificateLoader.Load());
        }
    });
});

builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(dataDirectory, "keys")));
// Pairing is authorized by the short-lived code shown by the local tray app.
// The token issued after pairing is still bound to the exact ERP origin, so
// deployed ERP hosts do not need to be added to every terminal's config first.
builder.Services.AddCors(cors => cors.AddDefaultPolicy(policy => policy
    .SetIsOriginAllowed(origin => Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
    .AllowAnyHeader()
    .AllowAnyMethod()
    .WithExposedHeaders("X-Print-Agent-Job-Id", "X-Print-Agent-Job-State", "X-Print-Agent-Queue-Latency-Ms")));
builder.Services.AddSingleton(allowedOriginStore);
builder.Services.AddSingleton<IppRequestParser>();
builder.Services.AddSingleton<AgentSecurityStore>();
builder.Services.AddSingleton<RouteStore>();
builder.Services.AddSingleton<PrintQueue>();
builder.Services.AddSingleton<IPrintTransport, WindowsRawPrintTransport>();
builder.Services.AddSingleton<IPrintTransport, TcpPrintTransport>();
builder.Services.AddHostedService<PrintQueueWorker>();
builder.Services.AddHostedService<NamedPipeManagementWorker>();

var app = builder.Build();
app.UseCors();
app.UseMiddleware<AgentAuthenticationMiddleware>();

app.MapGet("/api/v1/health", (PrintQueue queue, AgentSecurityStore security) =>
    new AgentHealth(
        Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0",
        true,
        security.IsPaired,
        queue.QueuedCount,
        DateTimeOffset.UtcNow));

app.MapPost("/api/v1/pair", async (PairRequest request, HttpContext context, AgentSecurityStore security) =>
{
    var requestOrigin = context.Request.Headers.Origin.FirstOrDefault();
    var requestedOrigin = Uri.TryCreate(request.Origin, UriKind.Absolute, out var requestUri)
        ? requestUri.GetLeftPart(UriPartial.Authority)
        : null;
    if (string.IsNullOrWhiteSpace(requestOrigin) ||
        !string.Equals(requestedOrigin, requestOrigin, StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new { error = "The ERP origin could not be verified." });
    }

    var token = await security.PairAsync(request.Code, request.Origin);
    return token is null ? Results.Unauthorized() : Results.Ok(new PairResponse(token));
});

app.MapGet("/api/v1/printers", () => WindowsPrinterDiscovery.GetPrinterNames());
app.MapGet("/api/v1/routes", (RouteStore routes) => routes.GetAll());
app.MapPut("/api/v1/routes", (IReadOnlyList<PrinterRoute> routes, RouteStore store) =>
{
    store.Replace(routes);
    return Results.NoContent();
});
app.MapGet("/api/v1/jobs", (PrintQueue queue) => queue.GetStatuses());
app.MapGet("/api/v1/jobs/{id:guid}", (Guid id, PrintQueue queue) =>
    queue.Find(id) is { } job ? Results.Ok(job.ToStatus()) : Results.NotFound());
app.MapPost("/api/v1/jobs/{id:guid}/retry", async (Guid id, PrintQueue queue) =>
{
    if (queue.Find(id) is not { } job) return Results.NotFound();
    try { await queue.RetryAsync(job); } catch (InvalidOperationException exception) { return Results.Conflict(new { error = exception.Message }); }
    return Results.Ok(job.ToStatus());
});
app.MapPost("/api/v1/jobs/{id:guid}/cancel", async (Guid id, PrintQueue queue) =>
{
    if (queue.Find(id) is not { } job) return Results.NotFound();
    try { await queue.CancelAsync(job); } catch (InvalidOperationException exception) { return Results.Conflict(new { error = exception.Message }); }
    return Results.Ok(job.ToStatus());
});
app.MapPost("/api/v1/security/revoke-all", (AgentSecurityStore security) =>
{
    security.RevokeAll();
    return Results.NoContent();
});

app.MapPost("/api/v1/test-print", async (TestPrintRequest request, HttpResponse response,
    PrintQueue queue, RouteStore routes) =>
{
    if (routes.Find(request.Route) is null) return Results.NotFound();
    var columns = request.PaperWidth == 58 ? 32 : 48;
    var text = "\u001b@\u001ba\u0001NEXTWAVE ERP\nPRINT AGENT TEST\n" + new string('-', columns) + "\n\n\n\u001dV\u0000";
    var enqueueStart = Stopwatch.GetTimestamp();
    var job = await queue.EnqueueAsync($"TEST-{Guid.NewGuid():N}", request.Route, Encoding.ASCII.GetBytes(text));
    response.Headers["X-Print-Agent-Job-Id"] = job.Id.ToString();
    response.Headers["X-Print-Agent-Job-State"] = job.State.ToString();
    response.Headers["X-Print-Agent-Queue-Latency-Ms"] =
        Stopwatch.GetElapsedTime(enqueueStart).TotalMilliseconds.ToString("F3", CultureInfo.InvariantCulture);
    return Results.Accepted($"/api/v1/jobs/{job.Id}", job.ToStatus());
});

app.MapPost("/ipp/print/{route}", async (string route, HttpRequest request, HttpResponse response,
    IppRequestParser parser, RouteStore routes, PrintQueue queue) =>
{
    if (routes.Find(route) is null)
    {
        return Results.NotFound();
    }

    await using var memory = new MemoryStream();
    await request.Body.CopyToAsync(memory, request.HttpContext.RequestAborted);
    IppPrintRequest ipp;
    try
    {
        ipp = parser.Parse(memory.ToArray());
    }
    catch (InvalidDataException exception)
    {
        return Results.BadRequest(new { error = exception.Message });
    }

    var externalId = ipp.JobName.StartsWith("POS-", StringComparison.OrdinalIgnoreCase) ? ipp.JobName[4..] : ipp.JobName;
    var enqueueStart = Stopwatch.GetTimestamp();
    var job = await queue.EnqueueAsync(externalId, route, ipp.Document);
    var enqueueElapsed = Stopwatch.GetElapsedTime(enqueueStart);
    response.Headers["X-Print-Agent-Job-Id"] = job.Id.ToString();
    response.Headers["X-Print-Agent-Job-State"] = job.State.ToString();
    response.Headers["X-Print-Agent-Queue-Latency-Ms"] =
        enqueueElapsed.TotalMilliseconds.ToString("F3", CultureInfo.InvariantCulture);
    return Results.Bytes(IppResponseWriter.Create(ipp.RequestId, job.Id, job.State), "application/ipp");
});

app.Run();

public partial class Program;
