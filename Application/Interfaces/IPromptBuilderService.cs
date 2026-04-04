using ComponentesIA.Domain.Entities;

namespace ComponentesIA.Application.Interfaces;

public interface IPromptBuilderService
{
    string BuildSystemPrompt(ExtractionTemplate template, IEnumerable<ExtractionField> fields);
    string BuildExpectedJsonSchema(IEnumerable<ExtractionField> fields);
}
