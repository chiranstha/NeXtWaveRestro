using System.Text.Json.Serialization;

namespace Nextwave.ERP.PrintAgent;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PrintJobState
{
    Accepted,
    Queued,
    Printing,
    Printed,
    Failed,
    Expired,
    Cancelled
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PrinterRouteKind
{
    WindowsQueue,
    Tcp9100
}

public sealed record PrinterRoute(
    string Name,
    PrinterRouteKind Kind,
    string? PrinterName = null,
    string? Host = null,
    int Port = 9100);

public sealed record PrintJobStatus(
    Guid Id,
    string ExternalJobId,
    string Route,
    PrintJobState State,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PrintedAt,
    int Attempts,
    string? LastError);

public sealed record AgentHealth(
    string Version,
    bool Ready,
    bool Paired,
    int QueuedJobs,
    DateTimeOffset ServerTime);

public sealed record PairRequest(string Code, string Origin);

public sealed record PairResponse(string Token);

public sealed record TestPrintRequest(string Route, int PaperWidth = 80);
