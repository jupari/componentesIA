namespace ComponentesIA.Application.Interfaces;

public interface IExtractionJobProcessor
{
    Task ProcessAsync(Guid jobId, CancellationToken ct = default);
}
