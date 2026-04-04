using System.Threading.Channels;
using ComponentesIA.Application.Interfaces;

namespace ComponentesIA.Infrastructure.Queue;

public class InMemoryJobDispatcher : IJobDispatcher
{
    private readonly Channel<Guid> _channel;
    private readonly ILogger<InMemoryJobDispatcher> _logger;

    public InMemoryJobDispatcher(Channel<Guid> channel, ILogger<InMemoryJobDispatcher> logger)
    {
        _channel = channel;
        _logger = logger;
    }

    public async Task DispatchAsync(Guid jobId, CancellationToken ct = default)
    {
        _logger.LogInformation("Encolando job {JobId}", jobId);
        await _channel.Writer.WriteAsync(jobId, ct);
    }
}
