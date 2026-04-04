using ComponentesIA.Models.DTOs;
using ComponentesIA.Services.Contracts;
using ComponentesIA.Helpers;

namespace ComponentesIA.Services;

/// <summary>
/// Implementación del servicio para la liquidación de nómina.
/// </summary>
public class PayrollService : IPayrollService
{
    private readonly ILogger<PayrollService> _logger;

    public PayrollService(ILogger<PayrollService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Orquesta el proceso de cálculo de la liquidación de nómina.
    /// </summary>
    /// <param name="request">Los datos de entrada para la liquidación.</param>
    /// <returns>El resultado detallado de la liquidación.</returns>
    public Task<PayrollResultDto> CalculatePayroll(PayrollRequestDto request)
    {
        _logger.LogInformation("Iniciando cálculo de nómina para un salario base de {BaseSalary}", request.BaseSalary);

        try
        {
            // 1. Calcular salario devengado según los días trabajados
            var earnedSalary = PayrollCalculator.CalculateEarnedSalary(request.BaseSalary, request.DaysWorked);

            // 2. Calcular el valor de la hora
            var hourlyRate = PayrollCalculator.CalculateHourlyRate(request.BaseSalary);

            // 3. Calcular el pago por horas extra
            var overtimePay = PayrollCalculator.CalculateOvertimePay(
                hourlyRate,
                request.DaytimeOvertimeHours,
                request.NighttimeOvertimeHours,
                request.HolidayOvertimeHours
            );

            // 4. Calcular el total devengado
            var totalAccrued = earnedSalary + overtimePay;

            // 5. Calcular deducciones de seguridad social
            var (healthDeduction, pensionDeduction) = PayrollCalculator.CalculateSocialSecurityDeductions(totalAccrued);
            var totalDeductions = healthDeduction + pensionDeduction;

            // 6. Calcular el neto a pagar
            var netPayable = totalAccrued - totalDeductions;

            // 7. Construir el objeto de respuesta
            var result = new PayrollResultDto
            {
                BaseSalary = request.BaseSalary,
                EarnedSalary = Math.Round(earnedSalary, 2),
                OvertimePay = Math.Round(overtimePay, 2),
                TotalAccrued = Math.Round(totalAccrued, 2),
                HealthInsuranceDeduction = Math.Round(healthDeduction, 2),
                PensionDeduction = Math.Round(pensionDeduction, 2),
                TotalDeductions = Math.Round(totalDeductions, 2),
                NetPayable = Math.Round(netPayable, 2)
            };

            _logger.LogInformation("Cálculo de nómina completado exitosamente.");

            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error durante el cálculo de la nómina.");
            // La excepción será capturada por el middleware global
            throw;
        }
    }
}
