using System.Net;
using System.Text.Json;
using Clean.Application.Abstractions;
using Clean.Application.Dtos.Assistant;
using Clean.Application.Dtos.Responses;
using Clean.Application.Services.Assistant;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Driver_Management.Controllers;

[ApiController]
[Route("api/assistant")]
[Authorize]
public class AssistantController : ControllerBase
{
    public const string RateLimitPolicy = "assistant";

    private readonly IAssistantService _assistantService;
    private readonly AssistantOptions _options;

    public AssistantController(IAssistantService assistantService, IOptions<AssistantOptions> options)
    {
        _assistantService = assistantService;
        _options = options.Value;
    }

    /// <summary>
    /// Ask the in-app assistant how a feature works. Streams the answer as Server-Sent Events:
    /// <c>data: {"delta":"..."}</c> per fragment, then <c>data: [DONE]</c>.
    /// </summary>
    [HttpPost("chat")]
    [EnableRateLimiting(RateLimitPolicy)]
    public async Task Chat([FromBody] AssistantChatRequestDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Question) || dto.Question.Length > _options.MaxQuestionLength)
        {
            Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await Response.WriteAsJsonAsync(new Response<string>(HttpStatusCode.BadRequest,
                $"Question must be 1–{_options.MaxQuestionLength} characters"), ct);
            return;
        }

        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no"; // don't let a reverse proxy buffer the stream

        try
        {
            await foreach (var delta in _assistantService.StreamAnswerAsync(dto, ct))
            {
                await Response.WriteAsync($"data: {JsonSerializer.Serialize(new { delta })}\n\n", ct);
                await Response.Body.FlushAsync(ct);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return; // client closed the panel
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Assistant] ❌ Chat error: {ex.Message}");
            await Response.WriteAsync(
                $"data: {JsonSerializer.Serialize(new { error = "The assistant is unavailable right now. Please try again later." })}\n\n", ct);
        }

        await Response.WriteAsync("data: [DONE]\n\n", ct);
    }
}
