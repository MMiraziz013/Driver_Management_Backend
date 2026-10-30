namespace Clean.Application.Services.Assistant;

public sealed class AssistantOptions
{
    public const string SectionName = "Assistant";

    // OpenAI-compatible endpoint root, e.g. Ollama at http://ollama:11434
    public string BaseUrl { get; init; } = "http://localhost:11434";
    public string Model { get; init; } = "qwen2.5:3b";
    public string Name { get; init; } = "Miraziz AI";

    // Folder of feature guides (*.md), relative to the app's base directory
    public string GuidePath { get; init; } = "AssistantGuide";

    public int MaxQuestionLength { get; init; } = 2000;
    public int MaxHistoryMessages { get; init; } = 10;
}
