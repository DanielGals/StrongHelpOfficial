using Microsoft.AspNetCore.Mvc;
using StrongHelpOfficial.Models;
using Microsoft.Data.SqlClient;

namespace StrongHelpOfficial.Controllers.Loaner
{
    [Area("Loaner")]
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
            var roleName = HttpContext.Session.GetString("RoleName") ?? "Unknown";
            var userId = HttpContext.Session.GetInt32("UserID") ?? 0;

            int documentCount = 0;
            using (var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                conn.Open();
                using (var cmd = new SqlCommand(
                    @"SELECT COUNT(*) 
              FROM LoanDocument d
              INNER JOIN LoanApplication a ON d.LoanID = a.LoanID
              WHERE a.UserID = @UserID", conn))
                {
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    documentCount = (int)cmd.ExecuteScalar();
                }
            }

            ViewData["Email"] = email;
            ViewData["RoleName"] = roleName;
            ViewData["UserID"] = userId;
            ViewData["DocumentCount"] = documentCount;

            // Pass a new model instance to the view
            var model = new ApplyForLoanViewModel();
            return View("~/Views/Loaner/ApplyForLoan.cshtml", model);
        }

        public IActionResult MyApplication()
        {
            var email = HttpContext.Session.GetString("Email") ?? "Unknown";
            var roleName = HttpContext.Session.GetString("RoleName") ?? "Unknown";
            var userId = HttpContext.Session.GetInt32("UserID") ?? 0;

            var model = new List<MyApplicationViewModel>();

            using (var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                conn.Open();
                using (var cmd = new SqlCommand(
                    @"SELECT LoanID, LoanAmount, DateSubmitted, ApplicationStatus, Title
              FROM LoanApplication
              WHERE UserID = @UserID
              ORDER BY DateSubmitted DESC", conn))
                {
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            model.Add(new MyApplicationViewModel
                            {
                                LoanID = reader.GetInt32(0),
                                LoanAmount = reader.GetDecimal(1),
                                DateSubmitted = reader.GetDateTime(2),
                                ApplicationStatus = reader.GetString(3),
                                Title = reader.GetString(4),
                                ProgressPercent = 0 // Set this based on your logic if needed
                            });
                        }
                    }
                }
            }

            ViewData["Email"] = email;
            ViewData["RoleName"] = roleName;
            ViewData["UserID"] = userId;

            return View("~/Views/Loaner/MyApplication.cshtml", model);
        }

        // Add this action for logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home", new { area = "" });
        }
    }
}
