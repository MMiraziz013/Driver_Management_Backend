using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace Clean.Application.Services.Assistant;

/// <summary>
/// Feature guides the assistant answers from, one subfolder per language (en, ru).
/// Read once from disk and kept in memory. Only these reviewed guides go into the prompt —
/// never source code or config.
/// </summary>
public class AssistantGuide
{
    public const string DefaultLanguage = "en";

    private readonly string _root;
    private readonly ConcurrentDictionary<string, string> _cache = new();

    public AssistantGuide(IOptions<AssistantOptions> options)
    {
        var guidePath = options.Value.GuidePath;
        _root = Path.IsPathRooted(guidePath)
            ? guidePath
            : Path.Combine(AppContext.BaseDirectory, guidePath);
    }

    /// <summary>
    /// The guide in the given language, or the English one when that language has none.
    /// A small model translates poorly, so answering from a guide already in the user's language matters.
    /// </summary>
    public string GetContent(string language)
    {
        var content = _cache.GetOrAdd(language, Load);
        return content.Length > 0 || language == DefaultLanguage
            ? content
            : GetContent(DefaultLanguage);
    }

    /// <summary>Russian guide for questions written in Cyrillic, English otherwise.</summary>
    public static string DetectLanguage(string question) =>
        question.Any(c => c is >= 'Ѐ' and <= 'ӿ') ? "ru" : DefaultLanguage;

    private string Load(string language)
    {
        var dir = Path.Combine(_root, language);
        if (!Directory.Exists(dir))
        {
            Console.WriteLine($"[Assistant] ⚠️ Guide folder not found: {dir}");
            return string.Empty;
        }

        var files = Directory.GetFiles(dir, "*.md").OrderBy(f => f, StringComparer.Ordinal);
        return string.Join("\n\n---\n\n", files.Select(File.ReadAllText));
    }
}
