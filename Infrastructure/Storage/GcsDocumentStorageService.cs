using ComponentesIA.Application.Interfaces;
using ComponentesIA.Models.Settings;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Options;

namespace ComponentesIA.Infrastructure.Storage;

public class GcsDocumentStorageService : IDocumentStorageService
{
    private readonly StorageClient _storageClient;
    private readonly string _bucketName;
    private readonly ILogger<GcsDocumentStorageService> _logger;

    public GcsDocumentStorageService(
        ILogger<GcsDocumentStorageService> logger,
        IOptions<GoogleCloudSettings> settings)
    {
        _logger = logger;
        _bucketName = settings.Value.BucketName;
        _storageClient = StorageClient.Create();
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default)
    {
        var objectName = $"documents/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}/{SanitizeFileName(fileName)}";

        _logger.LogInformation("Subiendo archivo {FileName} a GCS como {ObjectName}", fileName, objectName);

        await _storageClient.UploadObjectAsync(
            bucket: _bucketName,
            objectName: objectName,
            contentType: contentType,
            source: fileStream,
            cancellationToken: ct);

        _logger.LogInformation("Archivo subido exitosamente: {ObjectName}", objectName);
        return objectName;
    }

    public async Task<byte[]> DownloadAsync(string storagePath, CancellationToken ct = default)
    {
        _logger.LogInformation("Descargando archivo desde GCS: {StoragePath}", storagePath);

        using var memoryStream = new MemoryStream();
        await _storageClient.DownloadObjectAsync(
            bucket: _bucketName,
            objectName: storagePath,
            destination: memoryStream,
            cancellationToken: ct);

        return memoryStream.ToArray();
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat(fileName.Select(c => invalid.Contains(c) ? '_' : c));
    }
}
