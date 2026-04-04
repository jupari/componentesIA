using ComponentesIA.Models.DTOs;
using ComponentesIA.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace ComponentesIA.Controllers;

/// <summary>
/// Controlador para gestionar las operaciones relacionadas con la liquidación de nómina.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PayrollController : ControllerBase
{
    private readonly ILogger<PayrollController> _logger;
    private readonly IPayrollService _payrollService;

    public PayrollController(ILogger<PayrollController> logger, IPayrollService payrollService)
    {
        _logger = logger;
        _payrollService = payrollService;
    }

    /// <summary>
    /// Calcula la liquidación de nómina para un empleado.
    /// </summary>
    /// <param name="request">Datos de entrada como salario base, días trabajados y horas extra.</param>
    /// <returns>Un objeto con el detalle de la liquidación (devengos, deducciones y neto a pagar).</returns>
    [HttpPost("calculate")]
    [ProducesResponseType(typeof(PayrollResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CalculatePayroll([FromBody] PayrollRequestDto request)
    {
        _logger.LogInformation("Recibida solicitud de cálculo de nómina.");

        // El [ApiController] attribute se encarga de validar el modelo (DataAnnotations)
        // y devolverá un 400 Bad Request si no es válido.

        var result = await _payrollService.CalculatePayroll(request);

        return Ok(result);
    }
}
