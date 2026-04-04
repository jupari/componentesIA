using System.Text.Json.Serialization;

namespace ComponentesIA.Models;

/// <summary>
/// Representa el resultado de la extracción de datos de un certificado de Cámara de Comercio.
/// Las propiedades están basadas en la información comúnmente encontrada en dichos documentos en Colombia.
/// </summary>
public class CamaraComercioResult
{
    [JsonPropertyName("numero_matricula")]
    public string? NumeroMatricula { get; set; }

    [JsonPropertyName("nombre_empresa")]
    public string? NombreEmpresa { get; set; }

    [JsonPropertyName("nit")]
    public string? Nit { get; set; }

    [JsonPropertyName("domicilio")]
    public string? Domicilio { get; set; }

    [JsonPropertyName("municipio")]
    public string? Municipio { get; set; }

    [JsonPropertyName("direccion")]
    public string? Direccion { get; set; }

    [JsonPropertyName("correo_electronico")]
    public string? CorreoElectronico { get; set; }

    [JsonPropertyName("telefono")]
    public string? Telefono { get; set; }

    [JsonPropertyName("fecha_matricula")]
    public DateOnly? FechaMatricula { get; set; }

    [JsonPropertyName("fecha_renovacion")]
    public DateOnly? FechaRenovacion { get; set; }
    
    [JsonPropertyName("actividad_principal")]
    public string? ActividadPrincipal { get; set; }

    [JsonPropertyName("ciiu")]
    public string? Ciiu { get; set; }

    [JsonPropertyName("representante_legal")]
    public RepresentanteLegal? RepresentanteLegal { get; set; }
}

/// <summary>
/// Representa al representante legal de la empresa.
/// </summary>
public class RepresentanteLegal
{
    [JsonPropertyName("nombre")]
    public string? Nombre { get; set; }

    [JsonPropertyName("cedula")]
    public string? Cedula { get; set; }
}
