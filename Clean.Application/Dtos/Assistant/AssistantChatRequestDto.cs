namespace Clean.Application.Dtos.Assistant;

public class AssistantChatRequestDto
{
    public string Question { get; set; } = string.Empty;

    /// <summary>Earlier turns of this conversation, oldest first.</summary>
    public List<AssistantMessageDto> History { get; set; } = new();
}

public class AssistantMessageDto
{
    /// <summary>"user" or "assistant"</summary>
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
