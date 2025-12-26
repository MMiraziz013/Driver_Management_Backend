using System.Net;

namespace Clean.Application.Dtos.Responses;

public class Response<T>
{
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; }
    public T? Data { get; set; }

    public Response(HttpStatusCode statusCode, string message, T? data = default)
    {
        StatusCode = (int)statusCode;
        Message = message;
        Data = data;
    }
    public Response(HttpStatusCode statusCode, List<string> errors)
    {
        StatusCode = (int)statusCode;
        Errors = errors;
    }

    public Response(HttpStatusCode statusCode, T? data = default)
    {
        StatusCode = (int)statusCode;
        Data = data;
    }

    public Response(string message)
    {
        StatusCode = 200;
        Message = message;
    }
}