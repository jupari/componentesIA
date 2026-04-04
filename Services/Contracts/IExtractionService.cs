using ComponentesIA.Models;

namespace ComponentesIA.Services.Contracts;

/// <summary>
/// Define el contrato para el servicio de extracción de datos.
/// </summary>
public interface IExtractionService
{
    /// <summary>
    /// Extrae información de un documento de Cámara de Comercio.
    /// </summary>
    /// <param name="file">El archivo PDF del certificado.</param>
    /// <returns>Un objeto con la información extraída.</returns>
    Task<CamaraComercioResult?> ExtractDataFromCamaraComercio(IFormFile file);
}
