using Clean.Domain.Enums;

namespace Clean.Application.Dtos.User;

public class GetUserDto
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public bool IsActive { get; set; }
    public UserRole Role { get; set; }
}