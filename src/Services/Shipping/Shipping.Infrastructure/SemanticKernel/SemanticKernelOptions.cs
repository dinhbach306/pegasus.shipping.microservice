namespace Shipping.Infrastructure.SemanticKernel;

public sealed class SemanticKernelOptions
{
    public const string SectionName = "SemanticKernel";

    public string Provider { get; init; } = "OpenAI";
    public string ModelId { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
    public string? Endpoint { get; init; }
}

