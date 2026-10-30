using Clean.Application.Dtos.Assistant;

namespace Clean.Application.Abstractions;

public interface IAssistantService
{
    /// <summary>Streams the answer as text fragments, in order.</summary>
    IAsyncEnumerable<string> StreamAnswerAsync(AssistantChatRequestDto request, CancellationToken ct);
}
