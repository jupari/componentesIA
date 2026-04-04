using System.Text.RegularExpressions;

namespace ComponentesIA.Helpers;

/// <summary>
/// Proporciona métodos de utilidad para validaciones específicas de Colombia.
/// </summary>
public static class NitValidator
{
    /// <summary>
    /// Valida un Número de Identificación Tributaria (NIT) colombiano, incluyendo su dígito de verificación (DV).
    /// </summary>
    /// <param name="nitCompleto">El NIT a validar, que debe incluir el dígito de verificación (ej: "900123456-1").</param>
    /// <returns>True si el NIT y su DV son válidos, de lo contrario False.</returns>
    public static bool IsValid(string? nitCompleto)
    {
        if (string.IsNullOrWhiteSpace(nitCompleto))
        {
            return false;
        }

        // Normaliza el NIT: quita espacios, puntos, comas y lo convierte a mayúsculas.
        var nitNormalizado = nitCompleto.Replace(" ", "").Replace(".", "").Replace(",", "").ToUpper();

        // El formato debe ser NNNNNNNNN-D
        var match = Regex.Match(nitNormalizado, @"^(\d{1,15})-?(\d|K)$");

        if (!match.Success)
        {
            return false;
        }

        var nitSinDv = match.Groups[1].Value;
        var dvIngresado = match.Groups[2].Value;

        var dvCalculado = CalcularDigitoVerificacion(nitSinDv);

        return dvIngresado == dvCalculado;
    }

    /// <summary>
    /// Calcula el dígito de verificación para un número de NIT base.
    /// </summary>
    /// <param name="nitSinDv">El número de NIT sin el dígito de verificación.</param>
    /// <returns>El dígito de verificación calculado como un string.</returns>
    public static string CalcularDigitoVerificacion(string nitSinDv)
    {
        if (string.IsNullOrWhiteSpace(nitSinDv) || !nitSinDv.All(char.IsDigit))
        {
            throw new ArgumentException("El NIT base debe contener solo números.", nameof(nitSinDv));
        }

        int[] factores = { 71, 67, 59, 53, 47, 43, 41, 37, 29, 23, 19, 17, 13, 7, 3 };
        
        var nitArray = nitSinDv.PadLeft(15, '0').ToCharArray();
        int suma = 0;

        for (int i = 0; i < nitArray.Length; i++)
        {
            suma += (int)char.GetNumericValue(nitArray[i]) * factores[i];
        }

        int residuo = suma % 11;
        
        if (residuo < 2)
        {
            return residuo.ToString();
        }
        else
        {
            return (11 - residuo).ToString();
        }
    }
}
