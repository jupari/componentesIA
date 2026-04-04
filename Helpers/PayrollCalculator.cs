namespace ComponentesIA.Helpers;

/// <summary>
/// Proporciona métodos estáticos para cálculos de nómina según la ley colombiana.
/// </summary>
public static class PayrollCalculator
{
    // Constantes para los porcentajes de recargo de horas extra
    private const decimal DaytimeOvertimeRate = 1.25m;  // Recargo del 25%
    private const decimal NighttimeOvertimeRate = 1.75m; // Recargo del 75%
    private const decimal HolidayOvertimeRate = 2.00m;   // Recargo del 100% (75% festivo + 25% diurno)

    // Constantes para los porcentajes de deducción
    private const decimal HealthInsuranceRate = 0.04m; // 4%
    private const decimal PensionRate = 0.04m;         // 4%
    
    /// <summary>
    /// Calcula el valor de una hora de trabajo ordinaria.
    /// </summary>
    /// <param name="baseSalary">El salario mensual.</param>
    /// <returns>El valor de una hora de trabajo.</returns>
    public static decimal CalculateHourlyRate(decimal baseSalary)
    {
        if (baseSalary <= 0) return 0;
        // Se asumen 235 horas laborales al mes en Colombia.
        return baseSalary / 235m;
    }

    /// <summary>
    /// Calcula el valor total a pagar por horas extra.
    /// </summary>
    public static decimal CalculateOvertimePay(decimal hourlyRate, int daytimeHours, int nighttimeHours, int holidayHours)
    {
        var daytimePay = hourlyRate * daytimeHours * DaytimeOvertimeRate;
        var nighttimePay = hourlyRate * nighttimeHours * NighttimeOvertimeRate;
        var holidayPay = hourlyRate * holidayHours * HolidayOvertimeRate;
        
        return daytimePay + nighttimePay + holidayPay;
    }

    /// <summary>
    /// Calcula las deducciones de seguridad social (salud y pensión).
    /// </summary>
    /// <param name="totalAccrued">El total devengado por el empleado.</param>
    /// <returns>Un tuple con la deducción de salud y la de pensión.</returns>
    public static (decimal HealthDeduction, decimal PensionDeduction) CalculateSocialSecurityDeductions(decimal totalAccrued)
    {
        var health = totalAccrued * HealthInsuranceRate;
        var pension = totalAccrued * PensionRate;
        return (health, pension);
    }

    /// <summary>
    /// Calcula el salario devengado para un número específico de días.
    /// </summary>
    public static decimal CalculateEarnedSalary(decimal baseSalary, int daysWorked)
    {
        if (daysWorked < 1 || daysWorked > 30)
        {
            // Asumimos un mes de 30 días para cálculos de nómina
            throw new ArgumentException("Los días trabajados deben estar entre 1 y 30.", nameof(daysWorked));
        }
        return (baseSalary / 30m) * daysWorked;
    }
}
