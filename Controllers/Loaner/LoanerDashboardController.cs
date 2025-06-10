using Microsoft.AspNetCore.Mvc;
using StrongHelpOfficial.Models;
using Microsoft.Data.SqlClient;

namespace StrongHelpOfficial.Controllers.Loaner
{
    public class LoanerDashboardController : Controller
    {
        private readonly IConfiguration _config;

        public LoanerDashboardController(IConfiguration config)
        {
            _config = config;
        }

        public IActionResult Index()
        {
            var roleName = HttpContext.Session.GetString("RoleName");
            if (string.IsNullOrEmpty(roleName) || !(new[] { "Employee", "Benefits Assistant", "Approver" }.Contains(roleName)))
            {
                return RedirectToAction("Index", "Home");
            }

            var model = new LoanerDashboardViewModel();
            var email = HttpContext.Session.GetString("Email"); // Use session

            if (string.IsNullOrEmpty(email))
            {
                // Handle missing email (redirect, error, etc.)
                model.UserName = "Unknown User";
                return View("~/Views/Loaner/LoanerDashboard.cshtml", model);
            }

            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            int userId = 0;
            using (var cmd = new SqlCommand("SELECT UserID, FirstName, LastName FROM [User] WHERE Email = @Email", conn))
            {
                cmd.Parameters.AddWithValue("@Email", email);
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    userId = reader.GetInt32(0);
                    model.UserName = reader.GetString(1);
                }
                else
                {
                    model.UserName = "Unknown User";
                }
            }

            if (userId != 0)
            {
                using (var cmd = new SqlCommand("SELECT COUNT(*) FROM LoanApplication WHERE UserID = @UserID AND IsActive = 1", conn))
                {
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    model.ActiveLoans = (int)cmd.ExecuteScalar();
                }

                using (var cmd = new SqlCommand("SELECT COUNT(*) FROM LoanApplication WHERE UserID = @UserID AND ApplicationStatus IN ('Submitted', 'In Review')", conn))
                {
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    model.PendingApplications = (int)cmd.ExecuteScalar();
                }

                using (var cmd = new SqlCommand("SELECT TOP 5 CONCAT(Title, ' - ', ApplicationStatus, ' (', CONVERT(varchar, DateSubmitted, 120), ')') AS Activity FROM LoanApplication WHERE UserID = @UserID ORDER BY DateSubmitted DESC", conn))
                {
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    using var reader = cmd.ExecuteReader();
                    var activities = new List<string>();
                    while (reader.Read())
                    {
                        activities.Add(reader.GetString(0));
                    }
                    model.RecentActivities = activities;
                }
            }

            return View("~/Views/Loaner/LoanerDashboard.cshtml", model);
        }

        public IActionResult ApplyForLoan()
        {
            var email = HttpContext.Session.GetString("Email") ?? "Unknown";
            ViewData["Email"] = email;
            return View("~/Views/Loaner/ApplyForLoan.cshtml");
        }


        public IActionResult MyApplication()
        {
            var email = HttpContext.Session.GetString("Email") ?? "Unknown";
            ViewData["Email"] = email;
            return View("~/Views/Loaner/MyApplication.cshtml");
        }

        public IActionResult Logout(string? returnUrl = null)
        {
            HttpContext.Session.Clear();
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Index", "Home");
        }
    }
}

