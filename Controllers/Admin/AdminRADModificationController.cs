using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using StrongHelpOfficial.Models;
using Microsoft.Data.SqlClient;
using System;

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
        public IActionResult Index(string context, int id, bool edit = false, bool success = false)
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

            vm.EditMode = edit;
            vm.ShowSuccess = success;
            // Show deactivation success message if present
            if (TempData["DeactivationSuccess"] != null)
            {
                ViewBag.DeactivationSuccess = TempData["DeactivationSuccess"];
            }
            return View("~/Views/Admin/AdminRADModification.cshtml", vm);
        }

        [HttpPost]
        public IActionResult Save(AdminRADModificationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.EditMode = true;
                return View("~/Views/Admin/AdminRADModification.cshtml", model);
            }

            string table = model.Context == "Department" ? "Department" : "Role";
            string idColumn = table == "Department" ? "DepartmentId" : "RoleId";
            string nameColumn = table == "Department" ? "DepartmentName" : "RoleName";

            string? connectionString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("DefaultConnection connection string is not configured.");

            string modifiedBy = GetCurrentAdminName();

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();

                // Check for duplicate name (excluding current record)
                var checkCmd = new SqlCommand($@"
                    SELECT COUNT(*) FROM [{table}] WHERE {nameColumn} = @Name AND {idColumn} <> @Id", conn);
                checkCmd.Parameters.AddWithValue("@Name", model.Name);
                checkCmd.Parameters.AddWithValue("@Id", model.Id);
                int count = (int)checkCmd.ExecuteScalar();
                if (count > 0)
                {
                    ModelState.AddModelError("Name", $"{model.Context} name already exists.");
                    model.EditMode = true;
                    return View("~/Views/Admin/AdminRADModification.cshtml", model);
                }

                var cmd = new SqlCommand($@"
                    UPDATE [{table}]
                    SET {nameColumn} = @Name,
                        ModifiedAt = @ModifiedAt,
                        ModifiedBy = @ModifiedBy
                    WHERE {idColumn} = @Id", conn);

                cmd.Parameters.AddWithValue("@Name", model.Name);
                cmd.Parameters.AddWithValue("@ModifiedAt", DateTime.Now);
                cmd.Parameters.AddWithValue("@ModifiedBy", modifiedBy);
                cmd.Parameters.AddWithValue("@Id", model.Id);

                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Index", new { context = model.Context, id = model.Id, success = true });
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
