using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using StrongHelpOfficial.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;

namespace StrongHelpOfficial.Controllers.Admin
{
    public class AdminAddUserController : Controller
    {
        private readonly IConfiguration _configuration;
        public AdminAddUserController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var model = new AdminAddUserViewModel
            {
                Roles = GetRoles(),
                Departments = GetDepartments()
            };
            return View("~/Views/Admin/AdminAddUser.cshtml", model);
        }

        [HttpPost]
        public IActionResult Index(AdminAddUserViewModel model)
        {
            model.Roles = GetRoles();
            model.Departments = GetDepartments();

            if (!ModelState.IsValid)
            {
                return View("~/Views/Admin/AdminAddUser.cshtml", model);
            }

            string? connectionString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("DefaultConnection connection string is not configured.");

            // Check for duplicate email or contact number
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var checkCmd = new SqlCommand(@"
                    SELECT COUNT(*) FROM [User] 
                    WHERE Email = @Email OR ContactNum = @ContactNum", conn);
                checkCmd.Parameters.AddWithValue("@Email", model.Email);
                checkCmd.Parameters.AddWithValue("@ContactNum", model.ContactNum);
                int count = (int)checkCmd.ExecuteScalar();
                if (count > 0)
                {
                    ModelState.AddModelError("", "A user with this email or contact number already exists.");
                    return View("~/Views/Admin/AdminAddUser.cshtml", model);
                }

                // Get current admin name
                string createdBy = GetCurrentAdminName();

                var cmd = new SqlCommand(@"
                    INSERT INTO [User]
                        (FirstName, LastName, Email, ContactNum, RoleID, DepartmentID, DateHired, CreatedAt, CreatedBy, DepartmentAssignedAt, RoleAssignedAt, IsBanned, IsActive)
                    VALUES
                        (@FirstName, @LastName, @Email, @ContactNum, @RoleID, @DepartmentID, @DateHired, @CreatedAt, @CreatedBy, @DepartmentAssignedAt, @RoleAssignedAt, @IsBanned, @IsActive)", conn);

                cmd.Parameters.AddWithValue("@FirstName", model.FirstName);
                cmd.Parameters.AddWithValue("@LastName", model.LastName);
                cmd.Parameters.AddWithValue("@Email", model.Email);
                cmd.Parameters.AddWithValue("@ContactNum", model.ContactNum);
                cmd.Parameters.AddWithValue("@RoleID", model.RoleId);
                cmd.Parameters.AddWithValue("@DepartmentID", model.DepartmentId);
                cmd.Parameters.AddWithValue("@DateHired", model.DateHired);
                cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                cmd.Parameters.AddWithValue("@CreatedBy", createdBy);
                cmd.Parameters.AddWithValue("@DepartmentAssignedAt", DateTime.Now);
                cmd.Parameters.AddWithValue("@RoleAssignedAt", DateTime.Now);
                cmd.Parameters.AddWithValue("@IsBanned", 0);
                cmd.Parameters.AddWithValue("@IsActive", 1);

                cmd.ExecuteNonQuery();
            }

            ModelState.Clear();
            model = new AdminAddUserViewModel
            {
                Roles = GetRoles(),
                Departments = GetDepartments(),
                SuccessMessage = "User added successfully!"
            };
            return View("~/Views/Admin/AdminAddUser.cshtml", model);
        }

        private List<DropdownItem> GetRoles()
        {
            var roles = new List<DropdownItem>();
            string? connectionString = _configuration.GetConnectionString("DefaultConnection");
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT RoleID, RoleName FROM [Role] ORDER BY RoleName", conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        roles.Add(new DropdownItem
                        {
                            Id = reader["RoleID"].ToString() ?? "",
                            Name = reader["RoleName"]?.ToString() ?? ""
                        });
                    }
                }
            }
            return roles;
        }

        private List<DropdownItem> GetDepartments()
        {
            var departments = new List<DropdownItem>();
            string? connectionString = _configuration.GetConnectionString("DefaultConnection");
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT DepartmentID, DepartmentName FROM [Department] ORDER BY DepartmentName", conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        departments.Add(new DropdownItem
                        {
                            Id = reader["DepartmentID"].ToString() ?? "",
                            Name = reader["DepartmentName"]?.ToString() ?? ""
                        });
                    }
                }
            }
            return departments;
        }

        private string GetCurrentAdminName()
        {
            var firstName = HttpContext.Session.GetString("FirstName");
            var lastName = HttpContext.Session.GetString("LastName");
            if (!string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(lastName))
                return $"{firstName} {lastName}";
            if (!string.IsNullOrWhiteSpace(firstName))
                return firstName;
            return "System Admin";
        }
    }
}