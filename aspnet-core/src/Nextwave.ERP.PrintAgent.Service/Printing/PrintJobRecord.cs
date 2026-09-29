using Nextwave.ERP.PrintAgent;

namespace Nextwave.ERP.PrintAgent.Service.Printing;

public sealed class PrintJobRecord
{
    public Guid Id { get; set; }
    public string ExternalJobId { get; set; } = string.Empty;
    public string PayloadHash { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public PrintJobState State { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? PrintedAt { get; set; }
    public DateTimeOffset NextAttemptAt { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
    public byte[] Payload { get; set; } = [];

    public PrintJobStatus ToStatus() => new(Id, ExternalJobId, Route, State, CreatedAt, PrintedAt, Attempts, LastError);
}
