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
            string? connectionString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("DefaultConnection connection string is not configured.");
            }
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
                using (var command = new SqlCommand("SELECT COUNT(*) FROM [User] WHERE CreatedAt >= DATEADD(DAY, -30, GETDATE())", connection))
                {
                    model.UserCountLast30Days = (int)command.ExecuteScalar();
                }

                // Fetch the latest 3 users
                using (var command = new SqlCommand(@"
                    SELECT TOP 3 u.UserID, u.FirstName, u.LastName, r.RoleName, d.DepartmentName
                    FROM [User] u
                    LEFT JOIN [Role] r ON u.RoleID = r.RoleID
                    LEFT JOIN [Department] d ON u.DepartmentID = d.DepartmentID
                    WHERE u.CreatedAt >= DATEADD(DAY, -30, GETDATE())
                    ORDER BY u.CreatedAt DESC, u.UserID DESC", connection))
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
                            departments.Add(reader["DepartmentName"]?.ToString() ?? "");
                        }
                    }

                    model.UserID = userIds.ToArray();
                    model.UserName = userNames.ToArray();
                    model.RoleName = roleNames.ToArray();
                    model.RoleID = roleIds.ToArray();
                    model.Department = departments.ToArray();
                }

                var logs = new List<AdminDashboardLogEntry>();

                // Fetch user logs
                using (var command = new SqlCommand(@"
                    SELECT UserID, CONCAT(FirstName, ' ', LastName) AS Name, CreatedAt, ModifiedAt
                    FROM [User]", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var name = reader["Name"]?.ToString() ?? "";
                            var createdAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"));
                            DateTime? modifiedAt = reader.IsDBNull(reader.GetOrdinal("ModifiedAt"))
                                ? (DateTime?)null
                                : reader.GetDateTime(reader.GetOrdinal("ModifiedAt"));

                            logs.Add(new AdminDashboardLogEntry
                            {
                                EntityType = "User",
                                Name = name,
                                IsCreation = true,
                                ActionDate = createdAt
                            });
                            if (modifiedAt.HasValue && modifiedAt.Value > createdAt)
                            {
                                logs.Add(new AdminDashboardLogEntry
                                {
                                    EntityType = "User",
                                    Name = name,
                                    IsCreation = false,
                                    ActionDate = modifiedAt.HasValue ? modifiedAt.Value : default
                                });
                            }
                        }
                    }
                }

                // Fetch role logs
                using (var command = new SqlCommand(@"
                    SELECT RoleName AS Name, CreatedAt, ModifiedAt
                    FROM [Role]", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var name = reader["Name"]?.ToString() ?? "";
                            var createdAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"));
                            var modifiedAt = reader.IsDBNull(reader.GetOrdinal("ModifiedAt"))
                                ? (DateTime?)null
                                : reader.GetDateTime(reader.GetOrdinal("ModifiedAt"));

                            logs.Add(new AdminDashboardLogEntry
                            {
                                EntityType = "Role",
                                Name = name,
                                IsCreation = true,
                                ActionDate = createdAt
                            });
                            if (modifiedAt > createdAt)
                            {
                                logs.Add(new AdminDashboardLogEntry
                                {
                                    EntityType = "Role",
                                    Name = name,
                                    IsCreation = false,
                                    ActionDate = modifiedAt.HasValue ? modifiedAt.Value : default
                                });
                            }
                        }
                    }
                }

                // Fetch department logs
                using (var command = new SqlCommand(@"
                    SELECT DepartmentName AS Name, CreatedAt, ModifiedAt
                    FROM [Department]", connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var name = reader["Name"]?.ToString() ?? "";
                            var createdAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"));
                            var modifiedAt = reader.IsDBNull(reader.GetOrdinal("ModifiedAt"))
                                ? (DateTime?)null
                                : reader.GetDateTime(reader.GetOrdinal("ModifiedAt"));

                            logs.Add(new AdminDashboardLogEntry
                            {
                                EntityType = "Department",
                                Name = name,
                                IsCreation = true,
                                ActionDate = createdAt
                            });
                            if (modifiedAt > createdAt)
                            {
                                logs.Add(new AdminDashboardLogEntry
                                {
                                    EntityType = "Department",
                                    Name = name,
                                    IsCreation = false,
                                    ActionDate = modifiedAt.HasValue ? modifiedAt.Value : default
                                });
                            }
                        }
                    }
                }

                // Get the 3 most recent actions
                model.RecentLogs = logs
                    .OrderByDescending(l => l.ActionDate)
                    .Take(3)
                    .ToList();
            }

            return View("~/Views/Admin/AdminDashboard.cshtml", model);
        }
    }
}
