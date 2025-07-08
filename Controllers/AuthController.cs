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

    public IActionResult Login()
    {
        var userModel = new UserInfoViewModel
        {
            IsAuthenticated = User.Identity?.IsAuthenticated ?? false
        };

        // Attempt to connect to the SQL database regardless of AD authentication
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

        // Bypass AD authentication for testing
        userModel.IsAuthenticated = true; // Simulate authentication
        userModel.Email = "maria.santos@example.com"; // Hardcoded email for testing
        userModel.Username = "asdasd";
        userModel.Domain = "Sample Tae";

        // Check if Email exists in the SQL database table [User]
        if (!string.IsNullOrEmpty(userModel.Email))
        {
            try
            {
                using (var sqlConnection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    sqlConnection.Open();

                    // Updated query to join User and Role tables
                    var query = @"
                    SELECT u.UserID, r.RoleName, u.Email 
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

                                // Store SQL data in session using role name instead of role id.
                                HttpContext.Session.SetInt32("UserID", userId);
                                HttpContext.Session.SetString("RoleName", roleName);
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking email in SQL database.");
            }
        }

        // If the user is authenticated and the email matches SQL, redirect based on RoleName.
        if (userModel.IsAuthenticated && userModel.EmailExists && userModel.EmailMatched == true &&
            !string.IsNullOrEmpty(HttpContext.Session.GetString("RoleName")))
        {
            string roleName = HttpContext.Session.GetString("RoleName")!;
            if (roleName == "Employee") // Employee goes to Loaner Dashboard.
            {
                return RedirectToAction("Index", "LoanerDashboard", new { area = "Loaner" });
            }
            else if (roleName == "Benefits Assistant") // Benefits Assistant.
            {
                return RedirectToAction("Index", "BenefitsAssistantDashboard", new { area = "" });
            }
            else // All other roles go to Approver Dashboard.
            {
                return RedirectToAction("Index", "ApproverDashboard", new { area = "" });
            }
        }

        // Otherwise, display the login view.
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
