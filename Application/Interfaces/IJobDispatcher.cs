namespace ComponentesIA.Application.Interfaces;

public interface IJobDispatcher
{
    Task DispatchAsync(Guid jobId, CancellationToken ct = default);
}
