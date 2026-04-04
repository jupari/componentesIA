using ComponentesIA.Models.DTOs;
using ComponentesIA.Services.Contracts;
using Microsoft.AspNetCore.Mvc;
using ComponentesIA.Helpers;

namespace ComponentesIA.Controllers;

/// <summary>
/// Controlador para gestionar las operaciones relacionadas con la Cámara de Comercio.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CamaraComercioController : ControllerBase
{
    private readonly ILogger<CamaraComercioController> _logger;
    private readonly IExtractionService _extractionService;

    public CamaraComercioController(
        ILogger<CamaraComercioController> logger,
        IExtractionService extractionService)
    {
        _logger = logger;
        _extractionService = extractionService;
    }

    /// <summary>
    /// Recibe un documento PDF (certificado de Cámara de Comercio), extrae la información y la valida.
    /// </summary>
    /// <param name="request">La solicitud que contiene el archivo PDF.</param>
    /// <returns>Un objeto con la información extraída o un error si la validación falla.</returns>
    [HttpPost("upload-document")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UploadDocument([FromForm] UploadRequestDto request)
    {
        if (request.File == null || request.File.Length == 0)
        {
            _logger.LogWarning("Se intentó subir un archivo vacío.");
            return BadRequest("El archivo no puede estar vacío.");
        }
        
        if (request.File.ContentType != "application/pdf")
        {
            _logger.LogWarning("Se intentó subir un archivo que no es PDF: {ContentType}", request.File.ContentType);
            return BadRequest("El archivo debe ser un PDF.");
        }

        _logger.LogInformation("Archivo '{FileName}' recibido, iniciando procesamiento.", request.File.FileName);

        var result = await _extractionService.ExtractDataFromCamaraComercio(request.File);

        if (result == null)
        {
            _logger.LogError("No se pudo extraer información del archivo '{FileName}'.", request.File.FileName);
            return Problem("No se pudo procesar el documento o la extracción no arrojó resultados.");
        }
        
        // Valida el NIT extraído
        if (!string.IsNullOrEmpty(result.Nit) && !NitValidator.IsValid(result.Nit))
        {
            _logger.LogWarning("El NIT extraído '{Nit}' no es válido para el archivo '{FileName}'.", result.Nit, request.File.FileName);
            return BadRequest($"El NIT extraído ('{result.Nit}') no es válido según las reglas de la DIAN.");
        }
        
        _logger.LogInformation("El NIT '{Nit}' fue validado exitosamente.", result.Nit);

        return Ok(result);
    }
}
