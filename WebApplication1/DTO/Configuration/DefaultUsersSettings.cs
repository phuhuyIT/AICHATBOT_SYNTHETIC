namespace WebApplication1.DTO.Configuration;

public class DefaultUsersSettings
{
    public const string SectionName = "DefaultUsers";
    
    public UserSettings SystemUser { get; set; } = new();
    public UserSettings AdminUser { get; set; } = new();
}

public class UserSettings
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string RoleDescription { get; set; } = string.Empty;
}
