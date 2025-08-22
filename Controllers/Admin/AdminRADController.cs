using Microsoft.AspNetCore.Mvc;
using StrongHelpOfficial.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;

namespace StrongHelpOfficial.Controllers.Admin
{
    public class AdminRADController : Controller
    {
        private readonly IConfiguration _configuration;
        public AdminRADController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            var model = new AdminRADViewModel();
            model.Roles = GetRoles();
            model.Departments = GetDepartments();
            return View("~/Views/Admin/AdminRAD.cshtml", model);
        }

        public IActionResult Roles()
        {
            var model = new AdminRADViewModel();
            model.Roles = GetRoles();
            return View("~/Views/Admin/AdminRoles.cshtml", model);
        }

        public IActionResult Departments()
        {
            var model = new AdminRADViewModel();
            model.Departments = GetDepartments();
            return View("~/Views/Admin/AdminDepartments.cshtml", model);
        }

        private List<AdminRoleDto> GetRoles()
        {
            var roles = new List<AdminRoleDto>();
            string? connectionString = _configuration.GetConnectionString("DefaultConnection");
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT RoleID, RoleName, CreatedBy, CreatedAt, isActive FROM [Role]", conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        roles.Add(new AdminRoleDto
                        {
                            RoleID = reader.GetInt32(reader.GetOrdinal("RoleID")),
                            RoleName = reader["RoleName"]?.ToString() ?? "",
                            CreatedBy = reader["CreatedBy"]?.ToString() ?? "",
                            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                            IsActive = reader["isActive"] != DBNull.Value && Convert.ToInt32(reader["isActive"]) == 1
                        });
                    }
                }
            }
            return roles;
        }

        private List<AdminDepartmentDto> GetDepartments()
        {
            var departments = new List<AdminDepartmentDto>();
            string? connectionString = _configuration.GetConnectionString("DefaultConnection");
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT DepartmentID, DepartmentName, CreatedBy, CreatedAt, isActive FROM [Department]", conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        departments.Add(new AdminDepartmentDto
                        {
                            DepartmentID = reader.GetInt32(reader.GetOrdinal("DepartmentID")),
                            DepartmentName = reader["DepartmentName"]?.ToString() ?? "",
                            CreatedBy = reader["CreatedBy"]?.ToString() ?? "",
                            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                            IsActive = reader["isActive"] != DBNull.Value && Convert.ToInt32(reader["isActive"]) == 1
                        });
                    }
                }
            }
            return departments;
        }
    }
}
