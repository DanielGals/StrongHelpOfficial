namespace StrongHelpOfficial.Models;
public class UserInfoViewModel
{
    public bool IsAuthenticated { get; set; }
    public string? Username { get; set; }
    public string? Domain { get; set; }
    public string? DisplayName { get; set; }
    public string? Email { get; set; }

    public List<string> Roles { get; set; } = new();
    public List<UserInfoViewModel> AllUsers { get; set; } = new(); // New property for all users
}
