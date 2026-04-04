namespace ComponentesIA.Models.DTOs;

/// <summary>
/// Data Transfer Object para la solicitud de carga de archivos.
/// </summary>
public class UploadRequestDto
{
    /// <summary>
    /// Archivo a cargar. Debe ser un PDF para el caso de la Cámara de Comercio.
    /// </summary>
    public required IFormFile File { get; set; }
}
