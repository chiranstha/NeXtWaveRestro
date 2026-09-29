using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using Nextwave.ERP.PrintAgent.Service.Configuration;

namespace Nextwave.ERP.PrintAgent.Service.Printing;

public sealed class PrintQueue
{
    private readonly IDataProtector _protector;
    private readonly string _path;
    private readonly TimeSpan _jobLifetime;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly SemaphoreSlim _workSignal = new(0);
    private List<PrintJobRecord> _jobs;

    public PrintQueue(IDataProtectionProvider provider, IOptions<PrintAgentOptions> options)
    {
        _protector = provider.CreateProtector("Nextwave.ERP.PrintAgent.Queue.v1");
        _path = Path.Combine(options.Value.GetDataDirectory(), "queue.dat");
        _jobLifetime = TimeSpan.FromHours(Math.Clamp(options.Value.JobLifetimeHours, 1, 168));
        _jobs = Load();
    }

    public async Task<PrintJobRecord> EnqueueAsync(string externalJobId, string route, byte[] payload)
    {
        var hash = Convert.ToHexString(SHA256.HashData(payload));
        var added = false;
        await _gate.WaitAsync();
        try
        {
            var existing = _jobs.FirstOrDefault(job =>
                string.Equals(job.ExternalJobId, externalJobId, StringComparison.Ordinal) &&
                string.Equals(job.PayloadHash, hash, StringComparison.Ordinal));
            if (existing is not null)
            {
                return existing;
            }

            var job = new PrintJobRecord
            {
                Id = Guid.NewGuid(), ExternalJobId = externalJobId, PayloadHash = hash, Route = route,
                Payload = payload, State = PrintJobState.Accepted, CreatedAt = DateTimeOffset.UtcNow,
                NextAttemptAt = DateTimeOffset.UtcNow
            };
            _jobs.Add(job);
            Save();
            added = true;
            return job;
        }
        finally
        {
            _gate.Release();
            if (added)
            {
                _workSignal.Release();
            }
        }
    }

    public async Task<bool> WaitForWorkAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        return await _workSignal.WaitAsync(timeout, cancellationToken);
    }

    public IReadOnlyList<PrintJobStatus> GetStatuses() => _jobs.OrderByDescending(job => job.CreatedAt).Take(100).Select(job => job.ToStatus()).ToArray();
    public PrintJobRecord? Find(Guid id) => _jobs.FirstOrDefault(job => job.Id == id);
    public int QueuedCount => _jobs.Count(job => job.State is PrintJobState.Accepted or PrintJobState.Queued or PrintJobState.Printing);

    public async Task<IReadOnlyList<PrintJobRecord>> GetDueJobsAsync()
    {
        await _gate.WaitAsync();
        try
        {
            var now = DateTimeOffset.UtcNow;
            var changed = false;
            foreach (var job in _jobs.Where(job => job.State is PrintJobState.Accepted or PrintJobState.Queued or PrintJobState.Failed))
            {
                if (now - job.CreatedAt >= _jobLifetime)
                {
                    job.State = PrintJobState.Expired;
                    job.LastError = "The 24-hour print window expired.";
                    changed = true;
                }
            }

            if (changed)
            {
                Save();
            }
            return _jobs.Where(job =>
                (job.State is PrintJobState.Accepted or PrintJobState.Queued or PrintJobState.Failed) &&
                job.NextAttemptAt <= now).ToArray();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task UpdateAsync(PrintJobRecord job, PrintJobState state, string? error = null)
    {
        await _gate.WaitAsync();
        try
        {
            job.State = state;
            job.LastError = error;
            if (state == PrintJobState.Printed)
            {
                job.PrintedAt = DateTimeOffset.UtcNow;
                job.Payload = [];
            }
            else if (state == PrintJobState.Failed)
            {
                job.Attempts++;
                job.NextAttemptAt = DateTimeOffset.UtcNow.AddSeconds(Math.Min(300, Math.Pow(2, Math.Min(job.Attempts, 8))));
            }
            Save();
        }
        finally
        {
            _gate.Release();
            if (state is PrintJobState.Accepted or PrintJobState.Queued)
            {
                _workSignal.Release();
            }
        }
    }

    public Task RetryAsync(PrintJobRecord job)
    {
        if (job.State is PrintJobState.Printed or PrintJobState.Expired or PrintJobState.Cancelled)
        {
            throw new InvalidOperationException("This job can no longer be retried.");
        }
        job.NextAttemptAt = DateTimeOffset.UtcNow;
        return UpdateAsync(job, PrintJobState.Queued);
    }

    public Task CancelAsync(PrintJobRecord job)
    {
        if (job.State is PrintJobState.Printed or PrintJobState.Expired or PrintJobState.Cancelled)
        {
            throw new InvalidOperationException("This job can no longer be cancelled.");
        }
        return UpdateAsync(job, PrintJobState.Cancelled);
    }

    private List<PrintJobRecord> Load()
    {
        try
        {
            return File.Exists(_path)
                ? JsonSerializer.Deserialize<List<PrintJobRecord>>(_protector.Unprotect(File.ReadAllText(_path))) ?? []
                : [];
        }
        catch (Exception)
        {
            return [];
        }
    }

    private void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var temporaryPath = _path + ".tmp";
        File.WriteAllText(temporaryPath, _protector.Protect(JsonSerializer.Serialize(_jobs)));
        File.Move(temporaryPath, _path, true);
    }
}
