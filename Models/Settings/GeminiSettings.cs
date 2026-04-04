namespace ComponentesIA.Models.Settings;

/// <summary>
/// Contiene la configuración necesaria para conectarse a la API de Vertex AI (Gemini).
/// </summary>
public class GeminiSettings
{
    /// <summary>
    /// El ID del proyecto de Google Cloud.
    /// </summary>
    public required string ProjectId { get; set; }

    /// <summary>
    /// La ubicación del endpoint de Vertex AI (ej. "us-central1").
    /// </summary>
    public required string Location { get; set; }

    /// <summary>
    /// El publicador del modelo (ej. "google").
    /// </summary>
    public required string Publisher { get; set; }

    /// <summary>
    /// El nombre del modelo a utilizar (ej. "gemini-1.5-flash-001").
    /// </summary>
    public required string Model { get; set; }
}
