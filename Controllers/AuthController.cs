using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using StrongHelpOfficial.Models;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;

namespace StrongHelpOfficial.Controllers;

public class AuthController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IConfiguration _configuration;

    public AuthController(ILogger<HomeController> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public IActionResult Login(string? deactivated = null)
    {
        var userModel = new UserInfoViewModel
        {
            IsAuthenticated = User.Identity?.IsAuthenticated ?? false
        };

        // Test SQL database connection
        try
        {
            using (var sqlConnection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                sqlConnection.Open();
                userModel.SQLConnectionSuccess = true; // Connection successful
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to SQL database.");
            userModel.SQLConnectionSuccess = false; // Connection failed
        }

        // Only proceed if user is authenticated via Active Directory
        if (userModel.IsAuthenticated && User.Identity != null)
        {
            // Get user information from Active Directory
            string username = User.Identity.Name ?? string.Empty;
            userModel.Username = username;
            userModel.Domain = GetDomainFromUsername(username);

            // Get email from Active Directory
            userModel.Email = GetEmailFromActiveDirectory(username);

            // Check if Email exists in the SQL database table [User] and get IsActive
            if (!string.IsNullOrEmpty(userModel.Email))
            {
                try
                {
                    using (var sqlConnection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                    {
                        sqlConnection.Open();

                        // Updated query to join User and Role tables and get IsActive
                        var query = @"
                        SELECT u.UserID, r.RoleName, r.RoleID, u.Email, u.IsActive, u.FirstName, u.LastName
                        FROM [User] u
                        INNER JOIN [Role] r ON u.RoleID = r.RoleID
                        WHERE u.Email = @Email";

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
                                    string roleName = reader["RoleName"] as string ?? string.Empty;
                                    int roleId = reader.GetInt32(reader.GetOrdinal("RoleID"));
                                    bool isActive = reader["IsActive"] != DBNull.Value && Convert.ToBoolean(reader["IsActive"]);
                                    userModel.IsActive = isActive;

                                    // Store SQL data in session
                                    HttpContext.Session.SetInt32("UserID", userId);
                                    HttpContext.Session.SetString("RoleName", roleName);
                                    HttpContext.Session.SetInt32("RoleID", roleId);
                                    HttpContext.Session.SetString("Email", userModel.Email.ToLower());
                                    HttpContext.Session.SetString("FirstName", reader["FirstName"] as string ?? "");
                                    HttpContext.Session.SetString("LastName", reader["LastName"] as string ?? "");
                                }
                                else
                                {
                                    userModel.EmailExists = false;
                                    userModel.SQLEmail = null;
                                    userModel.EmailMatched = false;
                                    userModel.IsActive = null;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking email in SQL database.");
                }
            }

            // If user is deactivated, do not authenticate, show login page with deactivated message
            if (userModel.EmailExists == true && userModel.EmailMatched == true && userModel.IsActive == false)
            {
                userModel.IsAuthenticated = false;
                ViewBag.DeactivatedMessage = "Your account is deactivated. You cannot proceed to use the application. Please contact your administrator for assistance.";
                HttpContext.Session.SetString("IsDeactivated", "true");
                return View(userModel);
            }

            // If the user is authenticated and the email matches SQL, redirect based on RoleName
            if (userModel.IsAuthenticated && userModel.EmailExists == true && userModel.EmailMatched == true &&
                !string.IsNullOrEmpty(HttpContext.Session.GetString("RoleName")) && userModel.IsActive == true)
            {
                string roleName = HttpContext.Session.GetString("RoleName")!;

                if (roleName == "Employee") // Employee goes to Loaner Dashboard
                {
                    return RedirectToAction("Index", "LoanerDashboard", new { area = "Loaner" });
                }
                else if (roleName == "Benefits Assistant") // Benefits Assistant goes to selection page
                {
                    return RedirectToAction("Selection", "Auth");
                }
                else if (roleName == "Admin") // Admin goes to Admin Dashboard
                {
                    return RedirectToAction("Index", "AdminDashboard", new { area = "Admin" });
                }
                else // All other roles go to selection page if they are approvers
                {
                    // Check if the role is one of the approver roles
                    string[] approverRoles = new[] {
                        "Loans Division Approver",
                        "Specialized Accounting Approver",
                        "Compensation Management Approver",
                        "Benefits Services Officer",
                        "Benefit Management Department Head",
                        "Approving Officer",
                        "Final Disbursement Approver",
                        "Approver"
                    };

                    if (approverRoles.Contains(roleName))
                    {
                        return RedirectToAction("Selection", "Auth");
                    }
                    else
                    {
                        // If role doesn't match any known role, display login view with role info
                        return View(userModel);
                    }
                }
            }
        }

        // If not authenticated or user not found in database, display the login view
        return View(userModel);
    }

    public IActionResult Selection()
    {
        if (string.IsNullOrEmpty(HttpContext.Session.GetString("RoleName")))
        {
            return RedirectToAction("Login");
        }
        return View();
    }

    public IActionResult SwitchToEmployee()
    {
        if (string.IsNullOrEmpty(HttpContext.Session.GetString("RoleName")))
        {
            return RedirectToAction("Login");
        }
        return RedirectToAction("Index", "LoanerDashboard");
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

    private string GetEmailFromActiveDirectory(string username)
    {
        try
        {
            // Remove domain prefix if present
            string samAccountName = username.Contains('\\') ? username.Split('\\')[1] : username;

            using (var context = new PrincipalContext(ContextType.Domain))
            {
                using (var user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, samAccountName))
                {
                    return user?.EmailAddress ?? string.Empty;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving email from Active Directory for user: {Username}", username);
            return string.Empty;
        }
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