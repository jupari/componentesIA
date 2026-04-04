using System.ComponentModel.DataAnnotations;

namespace ComponentesIA.Models.DTOs;

/// <summary>
/// DTO para la solicitud de liquidación de nómina de un empleado.
/// </summary>
public class PayrollRequestDto
{
    /// <summary>
    /// Salario base mensual del empleado.
    /// </summary>
    [Required]
    [Range(1, double.MaxValue, ErrorMessage = "El salario base debe ser mayor a 0.")]
    public decimal BaseSalary { get; set; }

    /// <summary>
    /// Días trabajados en el período de liquidación.
    /// </summary>
    [Required]
    [Range(1, 31, ErrorMessage = "Los días trabajados deben estar entre 1 y 31.")]
    public int DaysWorked { get; set; }

    /// <summary>
    /// Número de horas extra diurnas trabajadas.
    /// </summary>
    [Range(0, 200, ErrorMessage = "Las horas extra no pueden ser negativas.")]
    public int DaytimeOvertimeHours { get; set; } = 0;

    /// <summary>
    /// Número de horas extra nocturnas trabajadas.
    /// </summary>
    [Range(0, 200, ErrorMessage = "Las horas extra no pueden ser negativas.")]
    public int NighttimeOvertimeHours { get; set; } = 0;

    /// <summary>
    /// Número de horas extra festivas/dominicales trabajadas.
    /// </summary>
    [Range(0, 200, ErrorMessage = "Las horas extra no pueden ser negativas.")]
    public int HolidayOvertimeHours { get; set; } = 0;
}


/// <summary>
/// DTO que representa el resultado de la liquidación de nómina.
/// </summary>
public class PayrollResultDto
{
    /// <summary>
    /// Salario base considerado para la liquidación.
    /// </summary>
    public decimal BaseSalary { get; set; }

    /// <summary>
    /// Salario devengado según los días trabajados.
    /// </summary>
    public decimal EarnedSalary { get; set; }

    /// <summary>
    /// Valor total pagado por horas extra.
    /// </summary>
    public decimal OvertimePay { get; set; }

    /// <summary>
    /// Total devengado (Salario + Horas Extra + Otros).
    /// </summary>
    public decimal TotalAccrued { get; set; }

    /// <summary>
    /// Aporte a salud del empleado (4%).
    /// </summary>
    public decimal HealthInsuranceDeduction { get; set; }

    /// <summary>
    /// Aporte a pensión del empleado (4%).
    /// </summary>
    public decimal PensionDeduction { get; set; }

    /// <summary>
    /// Total de deducciones.
    /// </summary>
    public decimal TotalDeductions { get; set; }

    /// <summary>
    /// Neto a pagar al empleado.
    /// </summary>
    public decimal NetPayable { get; set; }
}
