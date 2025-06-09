namespace StrongHelpOfficial.Models;
public class UserInfoViewModel
{
    public bool IsAuthenticated { get; set; }
    public string? Username { get; set; }
    public string? Domain { get; set; }
    public string? DisplayName { get; set; }
    public string? Email { get; set; }

    public List<string> Roles { get; set; } = new();
    public List<UserInfoViewModel> AllUsers { get; set; } = new();

    // New property to indicate if email exists in the SQL database
    public bool EmailExists { get; set; }

    // New properties for SQL email fetching and matching status
    public string? SQLEmail { get; set; }
    public bool? EmailMatched { get; set; }
    public bool SQLConnectionSuccess { get; set; }

}
