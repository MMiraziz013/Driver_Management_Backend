using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.Assistant;
using Microsoft.Extensions.Options;

namespace Clean.Application.Services.Assistant;

/// <summary>
/// Answers "how does this feature work" questions from the feature guides,
/// using a self-hosted model behind an OpenAI-compatible chat endpoint (Ollama).
/// </summary>
public class AssistantService : IAssistantService
{
    private readonly HttpClient _httpClient;
    private readonly AssistantOptions _options;
    private readonly AssistantGuide _guide;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public AssistantService(HttpClient httpClient, IOptions<AssistantOptions> options, AssistantGuide guide)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _guide = guide;
    }

    public async IAsyncEnumerable<string> StreamAnswerAsync(
        AssistantChatRequestDto request,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var body = new ChatCompletionRequest
        {
            Model = _options.Model,
            Stream = true,
            Temperature = 0.2,
            Messages = BuildMessages(_options, _guide.Content, request)
        };

        var url = $"{_options.BaseUrl.TrimEnd('/')}/v1/chat/completions";
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body, options: JsonOpts)
        };

        using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"[Assistant] ❌ Model HTTP {(int)response.StatusCode}");
            throw new HttpRequestException($"Assistant model returned {(int)response.StatusCode}");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (await reader.ReadLineAsync(ct) is { } line)
        {
            var delta = ParseStreamLine(line, out var done);
            if (done)
                yield break;
            if (!string.IsNullOrEmpty(delta))
                yield return delta;
        }
    }

    /// <summary>
    /// System prompt (rules + guides), then the sanitized history, then the new question.
    /// Clients can only send "user"/"assistant" turns — never their own system message.
    /// </summary>
    public static List<ChatMessage> BuildMessages(AssistantOptions options, string guide, AssistantChatRequestDto request)
    {
        var messages = new List<ChatMessage> { new("system", BuildSystemPrompt(options.Name, guide)) };

        var history = request.History
            .Where(m => m.Role is "user" or "assistant" && !string.IsNullOrWhiteSpace(m.Content))
            .TakeLast(options.MaxHistoryMessages)
            .Select(m => new ChatMessage(m.Role, Truncate(m.Content, options.MaxQuestionLength * 4)));
        messages.AddRange(history);

        messages.Add(new ChatMessage("user", Truncate(request.Question.Trim(), options.MaxQuestionLength)));
        return messages;
    }

    private static string BuildSystemPrompt(string name, string guide) =>
        $"""
        You are {name}, the help assistant inside the UVT Driver Management system.
        You explain to staff how the system's features work and how to use them.

        Rules:
        - Answer ONLY from the guide below. If the guide does not cover the question, say you don't know
          and suggest contacting the system administrator. Never guess numbers, formulas or rules.
        - Keep answers short and practical. Use the same names for screens, buttons and settings as the guide.
        - Answer in the language the user writes in (Uzbek, Russian or English).
        - You cannot see or change any data in the system. Don't claim to.

        ===== GUIDE =====
        {guide}
        ===== END OF GUIDE =====
        """;

    /// <summary>
    /// Parses one line of an OpenAI-style SSE stream. Returns the content fragment (or null),
    /// and sets <paramref name="done"/> when the stream ends.
    /// </summary>
    public static string? ParseStreamLine(string line, out bool done)
    {
        done = false;
        if (!line.StartsWith("data:", StringComparison.Ordinal))
            return null;

        var payload = line["data:".Length..].Trim();
        if (payload == "[DONE]")
        {
            done = true;
            return null;
        }

        var chunk = JsonSerializer.Deserialize<ChatCompletionChunk>(payload, JsonOpts);
        return chunk?.Choices?.FirstOrDefault()?.Delta?.Content;
    }

    private static string Truncate(string text, int max) => text.Length <= max ? text : text[..max];

    // ---------------- OpenAI-compatible wire types ----------------

    public record ChatMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private class ChatCompletionRequest
    {
        [JsonPropertyName("model")] public string Model { get; init; } = string.Empty;
        [JsonPropertyName("messages")] public List<ChatMessage> Messages { get; init; } = new();
        [JsonPropertyName("stream")] public bool Stream { get; init; }
        [JsonPropertyName("temperature")] public double? Temperature { get; init; }
    }

    private class ChatCompletionChunk
    {
        public List<ChunkChoice>? Choices { get; init; }
    }

    private class ChunkChoice
    {
        public ChunkDelta? Delta { get; init; }
    }

    private class ChunkDelta
    {
        public string? Content { get; init; }
    }
}
