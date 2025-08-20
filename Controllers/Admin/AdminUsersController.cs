using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using StrongHelpOfficial.Models;
using System.Collections.Generic;
using System;

namespace StrongHelpOfficial.Controllers.Admin
{
    public class AdminUsersController : Controller
    {
        private readonly IConfiguration _configuration;
        private const int PageSize = 5;

        public AdminUsersController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index(int page = 1, string? search = null, string? filter = null)
        {
            var model = new AdminUsersViewModel();
            var users = new List<AdminUsersViewModel.UserRow>();
            int totalUsers = 0;

            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            filter = string.IsNullOrEmpty(filter) ? "Name" : filter;
            search = search?.Trim() ?? "";

            // Build WHERE clause for search
            string whereClause = "";
            var parameters = new List<SqlParameter>();

            if (!string.IsNullOrEmpty(search))
            {
                switch (filter)
                {
                    case "UserID":
                        whereClause = "WHERE CAST(u.UserID AS VARCHAR) LIKE @search";
                        parameters.Add(new SqlParameter("@search", "%" + search + "%"));
                        break;
                    case "Name":
                        whereClause = "WHERE (u.FirstName + ' ' + u.LastName) LIKE @search";
                        parameters.Add(new SqlParameter("@search", "%" + search + "%"));
                        break;
                    case "Role":
                        whereClause = "WHERE r.RoleName LIKE @search";
                        parameters.Add(new SqlParameter("@search", "%" + search + "%"));
                        break;
                    case "Department":
                        whereClause = "WHERE d.DepartmentName LIKE @search";
                        parameters.Add(new SqlParameter("@search", "%" + search + "%"));
                        break;
                    default:
                        break;
                }
            }
            model.CurrentFilter = filter;
            model.CurrentSearch = search;

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Get total user count (with filter)
                string countQuery = $@"
                    SELECT COUNT(*)
                    FROM [User] u
                    LEFT JOIN [Role] r ON u.RoleID = r.RoleID
                    LEFT JOIN [Department] d ON u.DepartmentID = d.DepartmentID
                    {whereClause}";

                using (var countCmd = new SqlCommand(countQuery, connection))
                {
                    foreach (var p in parameters)
                        countCmd.Parameters.Add(p);

                    totalUsers = (int)countCmd.ExecuteScalar();
                }

                // Fetch paginated users with role and department
                string query = $@"
                    SELECT u.UserID, u.FirstName, u.LastName, r.RoleName, d.DepartmentName
                    FROM [User] u
                    LEFT JOIN [Role] r ON u.RoleID = r.RoleID
                    LEFT JOIN [Department] d ON u.DepartmentID = d.DepartmentID
                    {whereClause}
                    ORDER BY u.UserID
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

                using (var cmd = new SqlCommand(query, connection))
                {
                    foreach (var p in parameters)
                        cmd.Parameters.Add(new SqlParameter(p.ParameterName, p.Value));
                    cmd.Parameters.AddWithValue("@Offset", (page - 1) * PageSize);
                    cmd.Parameters.AddWithValue("@PageSize", PageSize);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            users.Add(new AdminUsersViewModel.UserRow
                            {
                                UserID = reader.GetInt32(reader.GetOrdinal("UserID")),
                                Name = $"{reader["FirstName"]} {reader["LastName"]}".Trim(),
                                Role = reader["RoleName"]?.ToString() ?? "",
                                Department = reader["DepartmentName"]?.ToString() ?? ""
                            });
                        }
                    }
                }
            }

            model.Users = users;
            model.CurrentPage = page;
            model.TotalUsers = totalUsers;
            model.TotalPages = (totalUsers + PageSize - 1) / PageSize;
            model.PageSize = PageSize;

            return View("~/Views/Admin/AdminUsers.cshtml", model);
        }
    }
}