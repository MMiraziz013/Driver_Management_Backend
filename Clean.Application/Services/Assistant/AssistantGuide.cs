using Microsoft.Extensions.Options;

namespace Clean.Application.Services.Assistant;

/// <summary>
/// Feature guides the assistant answers from. Read once from disk and kept in memory.
/// Only these reviewed guides go into the prompt — never source code or config.
/// </summary>
public class AssistantGuide
{
    private readonly Lazy<string> _content;

    public AssistantGuide(IOptions<AssistantOptions> options)
    {
        _content = new Lazy<string>(() => Load(options.Value.GuidePath));
    }

    public string Content => _content.Value;

    private static string Load(string guidePath)
    {
        var dir = Path.IsPathRooted(guidePath)
            ? guidePath
            : Path.Combine(AppContext.BaseDirectory, guidePath);

        if (!Directory.Exists(dir))
        {
            Console.WriteLine($"[Assistant] ⚠️ Guide folder not found: {dir}");
            return string.Empty;
        }

        var files = Directory.GetFiles(dir, "*.md").OrderBy(f => f, StringComparer.Ordinal);
        return string.Join("\n\n---\n\n", files.Select(File.ReadAllText));
    }
}
