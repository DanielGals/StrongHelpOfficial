using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using StrongHelpOfficial.Models;
using Microsoft.Data.SqlClient;
using System;

namespace StrongHelpOfficial.Controllers.Admin
{
    public class AdminRADCreationController : Controller
    {
        private readonly IConfiguration _configuration;
        public AdminRADCreationController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Index(string context)
        {
            ViewBag.Context = string.IsNullOrEmpty(context) ? "Role/Department" : context;
            return View("~/Views/Admin/AdminRADCreation.cshtml");
        }

        [HttpPost]
        public IActionResult Index(string context, string Name, string Description)
        {
            string createdBy = GetCurrentAdminName();

            string? connectionString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("DefaultConnection connection string is not configured.");

            string table = context?.ToLower() == "department" ? "Department" : "Role";
            string nameColumn = table == "Department" ? "DepartmentName" : "RoleName";

            // Check for duplicate name
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var checkCmd = new SqlCommand($@"
                    SELECT COUNT(*) FROM [{table}] WHERE {nameColumn} = @Name", conn);
                checkCmd.Parameters.AddWithValue("@Name", Name);
                int count = (int)checkCmd.ExecuteScalar();
                if (count > 0)
                {
                    ModelState.AddModelError("Name", $"{context} name already exists.");
                    ViewBag.Context = context;
                    return View("~/Views/Admin/AdminRADCreation.cshtml");
                }

                var cmd = new SqlCommand($@"
                    INSERT INTO [{table}] 
                        ({nameColumn}, CreatedBy, CreatedAt, isActive)
                    VALUES
                        (@Name, @CreatedBy, @CreatedAt, @IsActive)", conn);

                cmd.Parameters.AddWithValue("@Name", Name);
                cmd.Parameters.AddWithValue("@CreatedBy", createdBy);
                cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                cmd.Parameters.AddWithValue("@IsActive", 1);

                cmd.ExecuteNonQuery();
            }

            TempData["SuccessMessage"] = $"{context} created successfully!";
            return RedirectToAction("Index", new { context });
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
