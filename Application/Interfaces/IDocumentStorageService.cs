namespace ComponentesIA.Application.Interfaces;

public interface IDocumentStorageService
{
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default);
    Task<byte[]> DownloadAsync(string storagePath, CancellationToken ct = default);
}
