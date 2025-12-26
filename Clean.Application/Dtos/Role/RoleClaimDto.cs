namespace Clean.Application.Dtos.Role;

public class RoleClaimDto
{
    public string RoleId { get; set; }
    public string Type { get; set; }
    public string Value { get; set; }
    public bool Selected { get; set; }
    
    public RoleClaimDto()
    {
        
    }
    public RoleClaimDto(string type, string value, bool selected)
    {
        Type = type;
        Value = value;
        Selected = selected;
    }
    
    public RoleClaimDto(string type, string value)
    {
        Type = type;
        Value = value;
    }
}