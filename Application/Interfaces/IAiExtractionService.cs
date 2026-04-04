namespace ComponentesIA.Application.Interfaces;

public interface IAiExtractionService
{
    Task<string> ExtractAsync(
        byte[] fileBytes,
        string mimeType,
        string systemPrompt,
        string expectedJsonSchema,
        CancellationToken ct = default);
}
