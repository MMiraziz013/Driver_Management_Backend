namespace Clean.Application.Services.JWT;

public sealed class JwtOptions
{
    public const string SectionName = "JWT"; 
    public string Key { get; init; }
    public string Issuer { get; init; }
    public string Audience { get; init; }
    public int AccessTokenMinutes { get; init; }
}