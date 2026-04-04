using System.Text.Json;
using System.Text.RegularExpressions;
using ComponentesIA.Application.Interfaces;
using ComponentesIA.Domain.Entities;

namespace ComponentesIA.Application.UseCases;

public class ExtractionValidationService : IExtractionValidationService
{
    private readonly ILogger<ExtractionValidationService> _logger;

    public ExtractionValidationService(ILogger<ExtractionValidationService> logger)
    {
        _logger = logger;
    }

    public Dictionary<string, object?> ParseJson(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json);

            // Support both { "fields": {...} } and flat { "fieldKey": value }
            JsonElement root = doc.RootElement;
            if (root.TryGetProperty("fields", out var fieldsProp))
                root = fieldsProp;

            return root.EnumerateObject()
                .ToDictionary(
                    p => p.Name,
                    p => (object?)ExtractValue(p.Value));
        }
        catch (JsonException ex)
        {
            _logger.LogWarning("JSON de extracción malformado: {Message}", ex.Message);
            return new Dictionary<string, object?>();
        }
    }

    public List<(string FieldKey, bool IsValid, string? Message)> Validate(
        Dictionary<string, object?> parsed,
        IEnumerable<ExtractionField> fields)
    {
        var results = new List<(string, bool, string?)>();

        foreach (var field in fields)
        {
            parsed.TryGetValue(field.FieldKey, out var value);
            var strValue = value?.ToString();

            if (field.IsRequired && string.IsNullOrWhiteSpace(strValue))
            {
                results.Add((field.FieldKey, false, $"El campo '{field.FieldKey}' es requerido pero no fue encontrado."));
                continue;
            }

            if (string.IsNullOrWhiteSpace(strValue))
            {
                results.Add((field.FieldKey, true, null));
                continue;
            }

            if (!string.IsNullOrWhiteSpace(field.ValidationRegex))
            {
                var isMatch = Regex.IsMatch(strValue, field.ValidationRegex, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
                if (!isMatch)
                {
                    results.Add((field.FieldKey, false, $"El valor '{strValue}' no cumple el formato esperado para '{field.FieldKey}'."));
                    continue;
                }
            }

            results.Add((field.FieldKey, true, null));
        }

        return results;
    }

    public Dictionary<string, string?> Normalize(
        Dictionary<string, object?> parsed,
        IEnumerable<ExtractionField> fields)
    {
        var result = new Dictionary<string, string?>();

        foreach (var field in fields)
        {
            parsed.TryGetValue(field.FieldKey, out var value);
            var strValue = value?.ToString();

            if (string.IsNullOrWhiteSpace(strValue))
            {
                result[field.FieldKey] = null;
                continue;
            }

            strValue = ApplyNormalizeRules(strValue, field.NormalizeRule);
            result[field.FieldKey] = strValue;
        }

        return result;
    }

    private static string ApplyNormalizeRules(string value, string? rules)
    {
        if (string.IsNullOrWhiteSpace(rules)) return value.Trim();

        foreach (var rule in rules.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            value = rule.ToLowerInvariant() switch
            {
                "trim" => value.Trim(),
                "uppercase" => value.ToUpperInvariant(),
                "lowercase" => value.ToLowerInvariant(),
                "remove_currency" => Regex.Replace(value, @"[$\s.,]", "").Trim(),
                "clean_number" => Regex.Replace(value.Replace(",", "").Replace(".", ""), @"[^\d]", ""),
                _ => value
            };
        }

        return value;
    }

    private static object? ExtractValue(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number => element.TryGetDouble(out var d) ? d : (object?)element.GetRawText(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        JsonValueKind.Array => element.GetRawText(),
        JsonValueKind.Object => element.GetRawText(),
        _ => element.GetRawText()
    };
}
