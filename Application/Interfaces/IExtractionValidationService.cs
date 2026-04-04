using ComponentesIA.Domain.Entities;

namespace ComponentesIA.Application.Interfaces;

public interface IExtractionValidationService
{
    Dictionary<string, object?> ParseJson(string json);

    List<(string FieldKey, bool IsValid, string? Message)> Validate(
        Dictionary<string, object?> parsed,
        IEnumerable<ExtractionField> fields);

    Dictionary<string, string?> Normalize(
        Dictionary<string, object?> parsed,
        IEnumerable<ExtractionField> fields);
}
