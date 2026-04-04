using System.Threading.Channels;
using ComponentesIA.Application.Interfaces;

namespace ComponentesIA.Infrastructure.Queue;

public class JobProcessorBackgroundService : BackgroundService
{
    private readonly Channel<Guid> _channel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<JobProcessorBackgroundService> _logger;

    public JobProcessorBackgroundService(
        Channel<Guid> channel,
        IServiceScopeFactory scopeFactory,
        ILogger<JobProcessorBackgroundService> logger)
    {
        _channel = channel;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("JobProcessorBackgroundService iniciado.");

        await foreach (var jobId in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<IExtractionJobProcessor>();
                await processor.ProcessAsync(jobId, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error no controlado procesando job {JobId}", jobId);
            }
        }
    }
}
