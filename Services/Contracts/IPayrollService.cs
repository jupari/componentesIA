using ComponentesIA.Models.DTOs;

namespace ComponentesIA.Services.Contracts;

/// <summary>
/// Define el contrato para el servicio de liquidación de nómina.
/// </summary>
public interface IPayrollService
{
    /// <summary>
    /// Calcula la liquidación de nómina para un empleado basándose en la solicitud.
    /// </summary>
    /// <param name="request">Los datos para el cálculo de la nómina.</param>
    /// <returns>Un objeto con los resultados de la liquidación.</returns>
    Task<PayrollResultDto> CalculatePayroll(PayrollRequestDto request);
}
