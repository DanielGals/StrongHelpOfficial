using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using StrongHelpOfficial.Models;
using Microsoft.Data.SqlClient;
using System;
using Microsoft.AspNetCore.Http;

namespace StrongHelpOfficial.Controllers.Admin
{
    public class AdminRADModificationController : Controller
    {
        private readonly IConfiguration _configuration;
        public AdminRADModificationController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Index(string context, int id, bool success = false)
        {
            string table = context?.ToLower() == "department" ? "Department" : "Role";
            string idColumn = table == "Department" ? "DepartmentId" : "RoleId";
            string nameColumn = table == "Department" ? "DepartmentName" : "RoleName";

            var vm = new AdminRADModificationViewModel { Context = table };

            string? connectionString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("DefaultConnection connection string is not configured.");

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand($@"
                    SELECT {idColumn}, {nameColumn}, CreatedAt, CreatedBy, ModifiedAt, ModifiedBy, isActive
                    FROM [{table}]
                    WHERE {idColumn} = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", id);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        vm.Id = reader.GetInt32(0);
                        vm.Name = reader.GetString(1);
                        vm.CreatedAt = reader.GetDateTime(2);
                        vm.CreatedBy = reader.GetString(3);
                        vm.ModifiedAt = reader.IsDBNull(4) ? null : reader.GetDateTime(4);
                        vm.ModifiedBy = reader.IsDBNull(5) ? null : reader.GetString(5);
                        vm.IsActive = reader.GetBoolean(6);
                    }
                }
            }

            // Editing removed: always false
            vm.EditMode = false;
            vm.ShowSuccess = success;

            if (TempData["DeactivationSuccess"] != null)
            {
                ViewBag.ActionSuccess = TempData["DeactivationSuccess"];
            }
            if (TempData["ReactivationSuccess"] != null)
            {
                ViewBag.ActionSuccess = TempData["ReactivationSuccess"];
            }
            if (TempData["ActionError"] != null)
            {
                ViewBag.ActionError = TempData["ActionError"];
            }

            return View("~/Views/Admin/AdminRADModification.cshtml", vm);
        }

        [HttpPost]
        public IActionResult Deactivate(string context, int id)
        {
            string table = context?.ToLower() == "department" ? "Department" : "Role";
            string idColumn = table == "Department" ? "DepartmentId" : "RoleId";

            string? connectionString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("DefaultConnection connection string is not configured.");

            string modifiedBy = GetCurrentAdminName();

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();

                // Prevent deactivating the Admin role
                if (table == "Role")
                {
                    var checkCmd = new SqlCommand($@"SELECT RoleName FROM [Role] WHERE RoleId = @Id", conn);
                    checkCmd.Parameters.AddWithValue("@Id", id);
                    var roleNameObj = checkCmd.ExecuteScalar();
                    var roleName = roleNameObj as string ?? string.Empty;
                    if (string.Equals(roleName, "Admin", StringComparison.OrdinalIgnoreCase))
                    {
                        TempData["ActionError"] = "The Admin role cannot be deactivated.";
                        return RedirectToAction("Index", new { context, id });
                    }
                }

                var cmd = new SqlCommand($@"
                    UPDATE [{table}]
                    SET isActive = 0,
                        ModifiedAt = @ModifiedAt,
                        ModifiedBy = @ModifiedBy
                    WHERE {idColumn} = @Id", conn);

                cmd.Parameters.AddWithValue("@ModifiedAt", DateTime.Now);
                cmd.Parameters.AddWithValue("@ModifiedBy", modifiedBy);
                cmd.Parameters.AddWithValue("@Id", id);

                cmd.ExecuteNonQuery();
            }

            TempData["DeactivationSuccess"] = $"{(table == "Department" ? "Department" : "Role")} deactivated successfully!";
            return RedirectToAction("Index", new { context, id });
        }

        [HttpPost]
        public IActionResult Reactivate(string context, int id)
        {
            string table = context?.ToLower() == "department" ? "Department" : "Role";
            string idColumn = table == "Department" ? "DepartmentId" : "RoleId";

            string? connectionString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("DefaultConnection connection string is not configured.");

            string modifiedBy = GetCurrentAdminName();

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand($@"
                    UPDATE [{table}]
                    SET isActive = 1,
                        ModifiedAt = @ModifiedAt,
                        ModifiedBy = @ModifiedBy
                    WHERE {idColumn} = @Id", conn);

                cmd.Parameters.AddWithValue("@ModifiedAt", DateTime.Now);
                cmd.Parameters.AddWithValue("@ModifiedBy", modifiedBy);
                cmd.Parameters.AddWithValue("@Id", id);

                cmd.ExecuteNonQuery();
            }

            TempData["ReactivationSuccess"] = $"{(table == "Department" ? "Department" : "Role")} reactivated successfully!";
            return RedirectToAction("Index", new { context, id });
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