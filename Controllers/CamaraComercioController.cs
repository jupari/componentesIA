using ComponentesIA.Application.DTOs;
using ComponentesIA.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ComponentesIA.Controllers;

/// <summary>
/// Controlador legacy para procesamiento directo de Cámara de Comercio.
/// Se recomienda usar POST /api/extraction/batches con la plantilla correspondiente.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CamaraComercioController : ControllerBase
{
    private readonly ILogger<CamaraComercioController> _logger;
    private readonly IDocumentBatchService _batchService;

    public CamaraComercioController(
        ILogger<CamaraComercioController> logger,
        IDocumentBatchService batchService)
    {
        _logger = logger;
        _batchService = batchService;
    }

    /// <summary>
    /// Recibe un documento PDF (certificado de Cámara de Comercio) y lo procesa como lote de un documento.
    /// Requiere que exista una plantilla con código "camara_comercio".
    /// </summary>
    [HttpPost("upload-document")]
    [ProducesResponseType(typeof(BatchResponseDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadDocument(
        [FromForm] Guid templateId,
        [FromForm] IFormFile file,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest("El archivo no puede estar vacío.");

        if (file.ContentType != "application/pdf")
            return BadRequest("El archivo debe ser un PDF.");

        _logger.LogInformation("Archivo '{FileName}' recibido vía endpoint legacy.", file.FileName);

        try
        {
            var dto = new CreateBatchDto { TemplateId = templateId };
            var result = await _batchService.CreateBatchAsync(dto, new[] { file }, ct);
            return Accepted(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
