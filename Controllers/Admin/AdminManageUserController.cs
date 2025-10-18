using Microsoft.AspNetCore.Mvc;
using StrongHelpOfficial.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;

namespace StrongHelpOfficial.Controllers.Admin
{
    public class AdminManageUserController : Controller
    {
        private readonly IConfiguration _configuration;
        public AdminManageUserController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index(int userId)
        {
            var model = new AdminManageUserViewModel();
            string? connectionString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("DefaultConnection connection string is not configured.");

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand(@"
                    SELECT u.UserID, u.FirstName, u.LastName, u.Email, u.isActive AS Status, u.CreatedAt, u.ModifiedAt,
                           r.RoleName, r.RoleID, d.DepartmentName, d.DepartmentID
                    FROM [User] u
                    LEFT JOIN [Role] r ON u.RoleID = r.RoleID
                    LEFT JOIN [Department] d ON u.DepartmentID = d.DepartmentID
                    WHERE u.UserID = @UserID", conn);
                cmd.Parameters.AddWithValue("@UserID", userId);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        model.userId = reader["UserID"].ToString() ?? "";
                        model.firstName = reader["FirstName"]?.ToString() ?? "";
                        model.lastName = reader["LastName"]?.ToString() ?? "";
                        model.email = reader["Email"]?.ToString() ?? "";
                        model.isActive = reader["Status"] != DBNull.Value ? Convert.ToInt32(reader["Status"]) : 0;
                        model.role = reader["RoleID"]?.ToString() ?? ""; // ID
                        model.roleName = reader["RoleName"]?.ToString() ?? ""; // Name
                        model.department = reader["DepartmentID"]?.ToString() ?? ""; // ID
                        model.departmentName = reader["DepartmentName"]?.ToString() ?? ""; // Name
                        // Optionally for display:
                        ViewBag.RoleName = reader["RoleName"]?.ToString() ?? "";
                        ViewBag.DepartmentName = reader["DepartmentName"]?.ToString() ?? "";
                        model.createdAt = reader["CreatedAt"]?.ToString() ?? "";
                        model.modifiedAt = reader["ModifiedAt"]?.ToString() ?? "";
                    }
                }
            }
            return View("~/Views/Admin/AdminManageUser.cshtml", model);
        }

        [HttpGet]
        public JsonResult GetRoles()
        {
            var roles = new List<object>();
            string? connectionString = _configuration.GetConnectionString("DefaultConnection");
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT RoleID, RoleName FROM [Role] ORDER BY RoleName", conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        roles.Add(new { id = reader["RoleID"], name = reader["RoleName"] });
                    }
                }
            }
            return Json(roles);
        }

        [HttpGet]
        public JsonResult GetDepartments()
        {
            var departments = new List<object>();
            string? connectionString = _configuration.GetConnectionString("DefaultConnection");
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT DepartmentID, DepartmentName FROM [Department] ORDER BY DepartmentName", conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        departments.Add(new { id = reader["DepartmentID"], name = reader["DepartmentName"] });
                    }
                }
            }
            return Json(departments);
        }

        [HttpPost]
        public IActionResult Edit(AdminManageUserViewModel model)
        {
            string? connectionString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("DefaultConnection connection string is not configured.");

            // Fetch current data for comparison
            AdminManageUserViewModel current = new();
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand(@"
                    SELECT u.FirstName, u.LastName, u.Email, u.RoleID, u.DepartmentID, u.isActive AS Status
                    FROM [User] u WHERE u.UserID = @UserID", conn);
                cmd.Parameters.AddWithValue("@UserID", model.userId);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        current.firstName = reader["FirstName"]?.ToString() ?? "";
                        current.lastName = reader["LastName"]?.ToString() ?? "";
                        current.email = reader["Email"]?.ToString() ?? "";
                        current.role = reader["RoleID"]?.ToString() ?? "";
                        current.department = reader["DepartmentID"]?.ToString() ?? "";
                        current.isActive = reader["Status"] != DBNull.Value ? Convert.ToInt32(reader["Status"]) : 0;
                    }
                }
            }

            // Check if any data changed
            bool changed = model.firstName != current.firstName ||
                           model.lastName != current.lastName ||
                           model.email != current.email ||
                           model.role != current.role ||
                           model.department != current.department ||
                           model.isActive != current.isActive;

            if (!changed)
            {
                TempData["EditMessage"] = "No data changed";
                return RedirectToAction("Index", new { userId = model.userId });
            }

            // Update user info
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    var cmd = new SqlCommand(@"
                        UPDATE [User]
                        SET FirstName = @FirstName,
                            LastName = @LastName,
                            Email = @Email,
                            RoleID = @RoleID,
                            DepartmentID = @DepartmentID,
                            ModifiedAt = GETDATE()
                        WHERE UserID = @UserID", conn);
                    cmd.Parameters.AddWithValue("@FirstName", model.firstName);
                    cmd.Parameters.AddWithValue("@LastName", model.lastName);
                    cmd.Parameters.AddWithValue("@Email", model.email);

                    // Ensure these are IDs, not names!
                    if (!int.TryParse(model.role, out int roleId))
                        throw new Exception("Role ID is invalid.");
                    if (!int.TryParse(model.department, out int departmentId))
                        throw new Exception("Department ID is invalid.");

                    cmd.Parameters.AddWithValue("@RoleID", roleId);
                    cmd.Parameters.AddWithValue("@DepartmentID", departmentId);
                    cmd.Parameters.AddWithValue("@UserID", model.userId);

                    int rows = cmd.ExecuteNonQuery();
                    if (rows == 0)
                    {
                        TempData["EditMessage"] = "No rows updated. Please check the user ID.";
                    }
                    else
                    {
                        TempData["EditMessage"] = "User information updated successfully!";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["EditMessage"] = "Error updating user: " + ex.Message;
            }

            return RedirectToAction("Index", new { userId = model.userId });
        }

        [HttpPost]
        public IActionResult Deactivate(string userId)
        {
            string? connectionString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("DefaultConnection connection string is not configured.");

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand(@"
                    UPDATE [User]
                    SET isActive = 0, ModifiedAt = GETDATE()
                    WHERE UserID = @UserID", conn);
                cmd.Parameters.AddWithValue("@UserID", userId);
                cmd.ExecuteNonQuery();
            }

            TempData["EditMessage"] = "User has been deactivated.";
            return RedirectToAction("Index", new { userId });
        }

        [HttpPost]
        public IActionResult Reactivate(string userId)
        {
            string? connectionString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("DefaultConnection connection string is not configured.");

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand(@"
                    UPDATE [User]
                    SET isActive = 1, ModifiedAt = GETDATE()
                    WHERE UserID = @UserID", conn);
                cmd.Parameters.AddWithValue("@UserID", userId);
                cmd.ExecuteNonQuery();
            }

            // Note: User's session will be refreshed on their next login attempt
            TempData["EditMessage"] = "User has been reactivated. They may need to refresh their browser.";
            return RedirectToAction("Index", new { userId });
        }
    }
}
