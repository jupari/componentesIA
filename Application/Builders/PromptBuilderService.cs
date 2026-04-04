using System.Text;
using System.Text.Json;
using ComponentesIA.Application.Interfaces;
using ComponentesIA.Domain.Entities;

namespace ComponentesIA.Application.Builders;

public class PromptBuilderService : IPromptBuilderService
{
    public string BuildSystemPrompt(ExtractionTemplate template, IEnumerable<ExtractionField> fields)
    {
        var fieldList = fields.OrderBy(f => f.Order).ToList();
        var sb = new StringBuilder();

        sb.AppendLine($"Eres un asistente especializado en extracción de datos de documentos. Tu tarea es analizar el documento adjunto y extraer información estructurada de tipo '{template.Name}'.");
        sb.AppendLine();
        sb.AppendLine("CAMPOS A EXTRAER:");

        foreach (var field in fieldList)
        {
            var required = field.IsRequired ? " (REQUERIDO)" : " (opcional)";
            sb.AppendLine($"- {field.FieldKey} ({field.DataType}){required}: {field.Label}");

            if (!string.IsNullOrWhiteSpace(field.Description))
                sb.AppendLine($"  Descripción: {field.Description}");

            if (!string.IsNullOrWhiteSpace(field.AliasesJson))
            {
                try
                {
                    var aliases = JsonSerializer.Deserialize<List<string>>(field.AliasesJson);
                    if (aliases?.Count > 0)
                        sb.AppendLine($"  También puede aparecer como: {string.Join(", ", aliases)}");
                }
                catch { /* AliasesJson malformado — ignorar */ }
            }

            if (!string.IsNullOrWhiteSpace(field.ExampleValue))
                sb.AppendLine($"  Ejemplo: {field.ExampleValue}");
        }

        sb.AppendLine();
        sb.AppendLine("REGLAS:");
        sb.AppendLine("- Extrae ÚNICAMENTE los campos listados arriba.");
        sb.AppendLine("- Si no encuentras un campo, devuelve null para ese campo.");
        sb.AppendLine("- NO inventes valores. Si hay duda, prefiere null.");
        sb.AppendLine("- Para campos de tipo 'number': devuelve solo el número sin símbolos monetarios ni separadores de miles.");
        sb.AppendLine("- Para campos de tipo 'date': devuelve en formato ISO 8601 (YYYY-MM-DD).");
        sb.AppendLine("- Para campos de tipo 'array': devuelve un array JSON aunque esté vacío.");
        sb.AppendLine("- Incluye un campo 'fieldConfidence' paralelo con un valor entre 0.0 y 1.0 por cada campo extraído.");
        sb.AppendLine("- Responde ÚNICAMENTE con JSON válido. Sin texto adicional, sin bloques ```json, sin explicaciones.");

        if (!string.IsNullOrWhiteSpace(template.PromptStrategy))
        {
            sb.AppendLine();
            sb.AppendLine("INSTRUCCIONES ADICIONALES:");
            sb.AppendLine(template.PromptStrategy);
        }

        return sb.ToString();
    }

    public string BuildExpectedJsonSchema(IEnumerable<ExtractionField> fields)
    {
        var fieldList = fields.OrderBy(f => f.Order).ToList();

        var fieldsDict = new Dictionary<string, object?>();
        var confidenceDict = new Dictionary<string, object>();

        foreach (var field in fieldList)
        {
            fieldsDict[field.FieldKey] = field.DataType.ToLowerInvariant() switch
            {
                "array" => new List<object>(),
                "number" or "decimal" or "int" => (object?)null,
                _ => null
            };
            confidenceDict[field.FieldKey] = 0.0;
        }

        var schema = new
        {
            fields = fieldsDict,
            fieldConfidence = confidenceDict
        };

        return JsonSerializer.Serialize(schema, new JsonSerializerOptions { WriteIndented = true });
    }
}
