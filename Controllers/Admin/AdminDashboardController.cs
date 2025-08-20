using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using StrongHelpOfficial.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;

namespace StrongHelpOfficial.Controllers.Admin
{
    public class AdminDashboardController : Controller
    {
        private readonly IConfiguration _configuration;
        public AdminDashboardController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public IActionResult Index()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            var model = new AdminDashboardViewModel();

            // Fetch user count from database
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand("SELECT COUNT(*) FROM [User]", connection))
                {
                    model.UserCount = (int)command.ExecuteScalar();
                }
                using (var command = new SqlCommand("SELECT COUNT(*) FROM [User] WHERE isBanned = 0", connection))
                {
                    model.ActiveUserCount = (int)command.ExecuteScalar();
                }
                using (var command = new SqlCommand("SELECT COUNT(*) FROM [User] WHERE isBanned = 1", connection))
                {
                    model.InactiveUserCount = (int)command.ExecuteScalar();
                }
                using (var command = new SqlCommand("SELECT COUNT(*) FROM [User] WHERE DateCreated >= DATEADD(DAY, -30, GETDATE())", connection))
                {
                    model.UserCountLast30Days = (int)command.ExecuteScalar();
                }

                // Fetch the latest 3 users
                using (var command = new SqlCommand(@"
                    SELECT TOP 3 u.UserID, u.FirstName, u.LastName, r.RoleName
                    FROM [User] u
                    LEFT JOIN [Role] r ON u.RoleID = r.RoleID
                    WHERE u.DateCreated >= DATEADD(DAY, -30, GETDATE())
                    ORDER BY u.DateCreated DESC, u.UserID DESC", connection))
                {
                    var userIds = new List<int>();
                    var userNames = new List<string>();
                    var roleNames = new List<string>();
                    var roleIds = new List<string>();
                    var departments = new List<string>();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            userIds.Add(reader.GetInt32(reader.GetOrdinal("UserID")));
                            string firstName = reader["FirstName"]?.ToString() ?? "";
                            string lastName = reader["LastName"]?.ToString() ?? "";
                            userNames.Add($"{firstName} {lastName}".Trim());
                            roleNames.Add(reader["RoleName"]?.ToString() ?? "");
                            roleIds.Add(""); // Not used, but keep for compatibility
                            departments.Add(""); // No department yet
                        }
                    }

                    model.UserID = userIds.ToArray();
                    model.UserName = userNames.ToArray();
                    model.RoleName = roleNames.ToArray();
                    model.RoleID = roleIds.ToArray();
                    model.Department = departments.ToArray();
                }
            }

            

            return View("~/Views/Admin/AdminDashboard.cshtml", model);
        }
    }
}
