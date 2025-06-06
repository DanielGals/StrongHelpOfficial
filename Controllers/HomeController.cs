using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using Microsoft.AspNetCore.Mvc;
using StrongHelpOfficial.Models;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;



namespace StrongHelpOfficial.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    // Restrict access to users in "StrongHelp\\Admin" group
    [Authorize(Roles = "StrongHelp\\Admin")]
    public IActionResult Index()
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
