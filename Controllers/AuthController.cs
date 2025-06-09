using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using StrongHelpOfficial.Models;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;

namespace StrongHelpOfficial.Controllers;

// Restrict access to users in "StrongHelp\\Admin" group
//[Authorize(Roles = "StrongHelp\\Admin")]
public class AuthController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IConfiguration _configuration;

    public AuthController(ILogger<HomeController> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public IActionResult Login()
    {
        var userModel = new UserInfoViewModel
        {
            IsAuthenticated = User.Identity?.IsAuthenticated ?? false
        };

        if (userModel.IsAuthenticated && !string.IsNullOrEmpty(User.Identity?.Name))
        {
            userModel.Username = User.Identity.Name;
            userModel.Domain = GetDomainFromUsername(User.Identity.Name);

            // Get user identity parts
            var parts = User.Identity.Name.Split('\\');
            var domain = parts.Length > 1 ? parts[0] : Environment.UserDomainName;
            var username = parts.Length > 1 ? parts[1] : parts[0];

            try
            {
                using (var context = new PrincipalContext(ContextType.Domain, domain))
                using (var userPrincipal = UserPrincipal.FindByIdentity(context, username))
                {
                    if (userPrincipal != null)
                    {
                        userModel.DisplayName = userPrincipal.DisplayName;
                        userModel.Email = userPrincipal.EmailAddress;

                        // Fetch groups (roles) of the current user
                        var groups = userPrincipal.GetAuthorizationGroups();
                        foreach (var group in groups)
                        {
                            if (group is GroupPrincipal gp)
                            {
                                userModel.Roles.Add(gp.Name); // Add group names as roles
                            }
                        }

                        // Fetch all users in the domain
                        var allUsers = new List<UserInfoViewModel>();
                        using (var searcher = new PrincipalSearcher(new UserPrincipal(context)))
                        {
                            foreach (var result in searcher.FindAll())
                            {
                                if (result is UserPrincipal user)
                                {
                                    allUsers.Add(new UserInfoViewModel
                                    {
                                        Username = user.SamAccountName,
                                        DisplayName = user.DisplayName,
                                        Email = user.EmailAddress
                                    });
                                }
                            }
                        }
                        userModel.AllUsers = allUsers; // Add all users to the model

                        // Check if Email exists in the SQL database table [User]
                        if (!string.IsNullOrEmpty(userModel.Email))
                        {
                            using (var sqlConnection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                            {
                                sqlConnection.Open();
                                // Updated query to fetch UserID, RoleID, and Email
                                var query = "SELECT UserID, RoleID, Email FROM [User] WHERE Email = @Email";
                                using (var command = new SqlCommand(query, sqlConnection))
                                {
                                    command.Parameters.AddWithValue("@Email", userModel.Email);
                                    using (var reader = command.ExecuteReader())
                                    {
                                        if (reader.Read())
                                        {
                                            userModel.EmailExists = true;
                                            userModel.SQLEmail = reader["Email"] as string;
                                            userModel.EmailMatched = string.Equals(userModel.Email, userModel.SQLEmail, StringComparison.OrdinalIgnoreCase);

                                            int userId = reader.GetInt32(reader.GetOrdinal("UserID"));
                                            int roleId = reader.GetInt32(reader.GetOrdinal("RoleID"));

                                            // Store SQL data in session
                                            HttpContext.Session.SetInt32("UserID", userId);
                                            HttpContext.Session.SetString("RoleID", roleId.ToString());
                                            HttpContext.Session.SetString("Email", userModel.Email.ToLower());
                                        }
                                        else
                                        {
                                            userModel.EmailExists = false;
                                            userModel.SQLEmail = null;
                                            userModel.EmailMatched = false;
                                        }
                                    }
                                }
                            }
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching AD user details.");
            }
        }

        return View(userModel);
    }

    private string GetDomainFromUsername(string username)
    {
        if (string.IsNullOrEmpty(username))
            return string.Empty;

        if (username.Contains('\\'))
        {
            return username.Split('\\')[0];
        }

        return Environment.UserDomainName;
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
