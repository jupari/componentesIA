using ComponentesIA.Application.DTOs;
using ComponentesIA.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ComponentesIA.Controllers;

[ApiController]
[Route("api/extraction/templates")]
public class ExtractionTemplatesController : ControllerBase
{
    private readonly IExtractionTemplateService _service;

    public ExtractionTemplatesController(IExtractionTemplateService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<TemplateResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _service.GetAllAsync(ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TemplateResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(TemplateResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateTemplateDto dto, CancellationToken ct)
    {
        try
        {
            var result = await _service.CreateAsync(dto, ct);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TemplateResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTemplateDto dto, CancellationToken ct)
    {
        var result = await _service.UpdateAsync(id, dto, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:guid}/sample")]
    [ProducesResponseType(typeof(TemplateResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> UploadSample(Guid id, IFormFile file, CancellationToken ct)
    {
        var result = await _service.UploadSampleAsync(id, file, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{id:guid}/sample")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSample(Guid id, CancellationToken ct)
    {
        var result = await _service.GetSampleAsync(id, ct);
        if (result is null) return NotFound();
        return File(result.Value.Bytes, result.Value.ContentType, result.Value.FileName);
    }
}
