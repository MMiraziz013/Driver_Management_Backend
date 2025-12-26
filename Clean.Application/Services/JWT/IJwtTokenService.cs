namespace Clean.Application.Services.JWT;

public interface IJwtTokenService
{
    public Task<string> GenerateJwtToken(Domain.Entities.User user);
    
}