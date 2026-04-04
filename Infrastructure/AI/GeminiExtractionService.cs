using ComponentesIA.Application.Interfaces;
using ComponentesIA.Models.Settings;
using Google.Cloud.AIPlatform.V1;
using Google.Protobuf;
using Microsoft.Extensions.Options;

namespace ComponentesIA.Infrastructure.AI;

public class GeminiExtractionService : IAiExtractionService
{
    private readonly ILogger<GeminiExtractionService> _logger;
    private readonly PredictionServiceClient _predictionServiceClient;
    private readonly GeminiSettings _geminiSettings;

    public GeminiExtractionService(
        ILogger<GeminiExtractionService> logger,
        IOptions<GeminiSettings> geminiSettings,
        PredictionServiceClient predictionServiceClient)
    {
        _logger = logger;
        _geminiSettings = geminiSettings.Value;
        _predictionServiceClient = predictionServiceClient;
    }

    public async Task<string> ExtractAsync(
        byte[] fileBytes,
        string mimeType,
        string systemPrompt,
        string expectedJsonSchema,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Enviando solicitud a Vertex AI (Gemini). MimeType: {MimeType}", mimeType);

        var endpoint = EndpointName.FromProjectLocationPublisherModel(
            _geminiSettings.ProjectId,
            _geminiSettings.Location,
            _geminiSettings.Publisher,
            _geminiSettings.Model);

        var userPrompt = $"Extrae la información del documento siguiendo exactamente este esquema JSON:\n{expectedJsonSchema}";

        var request = new GenerateContentRequest
        {
            Model = endpoint.ToString(),
            SystemInstruction = new Content
            {
                Parts = { new Part { Text = systemPrompt } }
            },
            Contents =
            {
                new Content
                {
                    Role = "USER",
                    Parts =
                    {
                        new Part { Text = userPrompt },
                        new Part
                        {
                            InlineData = new()
                            {
                                MimeType = mimeType,
                                Data = ByteString.CopyFrom(fileBytes)
                            }
                        }
                    }
                }
            }
        };

        var response = await _predictionServiceClient.GenerateContentAsync(request, ct);

        var candidate = response.Candidates.FirstOrDefault();
        if (candidate is null || !candidate.Content.Parts.Any())
        {
            _logger.LogWarning("La respuesta de Vertex AI no contiene candidatos válidos.");
            throw new InvalidOperationException("El modelo no devolvió ningún candidato.");
        }

        var rawJson = candidate.Content.Parts.First().Text;
        var cleanJson = CleanJsonResponse(rawJson);

        _logger.LogInformation("Respuesta JSON recibida de Gemini ({Length} chars).", cleanJson.Length);
        return cleanJson;
    }

    private string CleanJsonResponse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

        var startIndex = raw.IndexOf('{');
        var endIndex = raw.LastIndexOf('}');

        if (startIndex == -1 || endIndex == -1 || endIndex < startIndex)
        {
            _logger.LogWarning("No se encontró un objeto JSON válido en la respuesta de Gemini.");
            return string.Empty;
        }

        return raw.Substring(startIndex, endIndex - startIndex + 1);
    }
}
