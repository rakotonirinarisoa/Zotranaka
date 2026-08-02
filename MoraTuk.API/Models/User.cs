namespace MoraTuk.API.Models;

public class User
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string Role { get; set; } = "Client";
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}