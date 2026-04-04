using System.Text.Json;
using ComponentesIA.Models;
using ComponentesIA.Models.Settings;
using ComponentesIA.Services.Contracts;
using Google.Cloud.AIPlatform.V1;
using Google.Protobuf;
using Microsoft.Extensions.Options;

namespace ComponentesIA.Services;

/// <summary>
/// Implementación del servicio de extracción que utiliza la IA de Google (Gemini) a través de Vertex AI.
/// </summary>
public class GeminiExtractionService : IExtractionService
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

    /// <summary>
    /// Extrae datos de un PDF de Cámara de Comercio usando el modelo Gemini 1.5 Flash.
    /// </summary>
    /// <param name="file">El archivo PDF a procesar.</param>
    /// <returns>El resultado del procesamiento o null si falla.</returns>
    public async Task<CamaraComercioResult?> ExtractDataFromCamaraComercio(IFormFile file)
    {
        _logger.LogInformation("Iniciando extracción de datos para el archivo: {FileName}", file.FileName);

        if (file.ContentType != "application/pdf")
        {
            _logger.LogWarning("El archivo {FileName} no es un PDF.", file.FileName);
            throw new ArgumentException("El archivo debe ser un PDF.");
        }

        try
        {
            var endpoint = EndpointName.FromProjectLocationPublisherModel(
                _geminiSettings.ProjectId,
                _geminiSettings.Location,
                _geminiSettings.Publisher,
                _geminiSettings.Model);

            // Convierte el IFormFile a un array de bytes para la API
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            var fileBytes = memoryStream.ToArray();

            // Define el prompt que le indica al modelo qué hacer
            var prompt = "Extrae la siguiente información del certificado de cámara de comercio adjunto y devuélvela en formato JSON. Asegúrate de que el JSON esté limpio y no contenga caracteres extraños ni '```json'. Los campos son: numero_matricula, nombre_empresa, nit, domicilio, municipio, direccion, correo_electronico, telefono, fecha_matricula (en formato YYYY-MM-DD), fecha_renovacion (en formato YYYY-MM-DD), actividad_principal, ciiu, y el representante_legal con su nombre y cedula.";

            // Construye la solicitud para el modelo Gemini usando la API GenerateContent
            var generateContentRequest = new GenerateContentRequest
            {
                Model = endpoint.ToString(), // El endpoint completo también se usa como nombre del modelo
                Contents =
                {
                    new Content
                    {
                        Role = "USER",
                        Parts =
                        {
                            new Part { Text = prompt },
                            new Part
                            {
                                InlineData = new()
                                {
                                    MimeType = "application/pdf",
                                    Data = ByteString.CopyFrom(fileBytes)
                                }
                            }
                        }
                    }
                }
            };

            // Llama a la API de Vertex AI
            _logger.LogInformation("Enviando solicitud a Vertex AI para el modelo Gemini.");
            var response = await _predictionServiceClient.GenerateContentAsync(generateContentRequest);
            
            // Procesa la respuesta
            var candidate = response.Candidates.FirstOrDefault();
            if (candidate == null || !candidate.Content.Parts.Any())
            {
                _logger.LogWarning("La respuesta de Vertex AI no contiene un candidato o partes válidas.");
                return null;
            }

            var jsonResponse = candidate.Content.Parts.First().Text;
            var cleanJson = CleanGeminiResponse(jsonResponse);

            _logger.LogInformation("Respuesta JSON recibida y limpiada de Gemini.");
            
            // Deserializa el JSON a nuestro objeto de resultado
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = JsonSerializer.Deserialize<CamaraComercioResult>(cleanJson, options);

            _logger.LogInformation("Extracción completada exitosamente para {FileName}.", file.FileName);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante la extracción de datos con Gemini.");
            throw; // Relanza la excepción para que el middleware la maneje
        }
    }

    /// <summary>
    /// Limpia el string JSON devuelto por Gemini, quitando los marcadores de bloque de código.
    /// </summary>
    private string CleanGeminiResponse(string rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson)) return string.Empty;

        // Busca el primer '{' y el último '}' para extraer el objeto JSON
        var startIndex = rawJson.IndexOf('{');
        var endIndex = rawJson.LastIndexOf('}');
        
        if (startIndex == -1 || endIndex == -1 || endIndex < startIndex)
        {
            _logger.LogWarning("No se encontró un objeto JSON válido en la respuesta: {rawJson}", rawJson);
            return string.Empty;
        }
        
        return rawJson.Substring(startIndex, endIndex - startIndex + 1);
    }
}
