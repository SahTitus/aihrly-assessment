using System.Threading.Channels;

namespace Aihrly.Api.Services;

public record NotificationJob(Guid ApplicationId, string Type);

// unbounded channel is fine here, load is low
public sealed class NotificationQueue
{
    private readonly Channel<NotificationJob> _channel = Channel.CreateUnbounded<NotificationJob>();

    public void Enqueue(NotificationJob job) => _channel.Writer.TryWrite(job);

    public IAsyncEnumerable<NotificationJob> ReadAllAsync(CancellationToken ct)
        => _channel.Reader.ReadAllAsync(ct);
}
