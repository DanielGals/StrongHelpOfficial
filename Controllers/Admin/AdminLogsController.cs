using Microsoft.AspNetCore.Mvc;
using StrongHelpOfficial.Models;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;

namespace StrongHelpOfficial.Controllers.Admin
{
    public class AdminLogsController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly string? _connectionString;

        public AdminLogsController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
        }

        public IActionResult Index(DateTime? fromDate, DateTime? toDate, int page = 1)
        {
            const int pageSize = 10;
            var logs = new List<AdminLogEntryViewModel>();
            int totalLogs = 0;

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                // Combined count query for all entities
                using (var countCmd = conn.CreateCommand())
                {
                    countCmd.CommandText = @"
                        SELECT COUNT(*) FROM (
                            SELECT UserID AS Id, 'User' AS EntityType, CreatedAt AS ActionDate FROM [User]
                            WHERE (@fromDate IS NULL OR CreatedAt >= @fromDate)
                              AND (@toDate IS NULL OR CreatedAt <= @toDate)
                            UNION ALL
                            SELECT UserID AS Id, 'User' AS EntityType, ModifiedAt AS ActionDate FROM [User]
                            WHERE ModifiedAt IS NOT NULL
                              AND (@fromDate IS NULL OR ModifiedAt >= @fromDate)
                              AND (@toDate IS NULL OR ModifiedAt <= @toDate)
                            UNION ALL
                            SELECT RoleID AS Id, 'Role' AS EntityType, CreatedAt AS ActionDate FROM [Role]
                            WHERE (@fromDate IS NULL OR CreatedAt >= @fromDate)
                              AND (@toDate IS NULL OR CreatedAt <= @toDate)
                            UNION ALL
                            SELECT RoleID AS Id, 'Role' AS EntityType, ModifiedAt AS ActionDate FROM [Role]
                            WHERE ModifiedAt IS NOT NULL
                              AND (@fromDate IS NULL OR ModifiedAt >= @fromDate)
                              AND (@toDate IS NULL OR ModifiedAt <= @toDate)
                            UNION ALL
                            SELECT DepartmentID AS Id, 'Department' AS EntityType, CreatedAt AS ActionDate FROM [Department]
                            WHERE (@fromDate IS NULL OR CreatedAt >= @fromDate)
                              AND (@toDate IS NULL OR CreatedAt <= @toDate)
                            UNION ALL
                            SELECT DepartmentID AS Id, 'Department' AS EntityType, ModifiedAt AS ActionDate FROM [Department]
                            WHERE ModifiedAt IS NOT NULL
                              AND (@fromDate IS NULL OR ModifiedAt >= @fromDate)
                              AND (@toDate IS NULL OR ModifiedAt <= @toDate)
                        ) AS AllLogs
                    ";
                    countCmd.Parameters.AddWithValue("@fromDate", (object?)fromDate ?? DBNull.Value);
                    countCmd.Parameters.AddWithValue("@toDate", (object?)toDate ?? DBNull.Value);
                    totalLogs = (int)countCmd.ExecuteScalar();
                }

                // Combined paged query for all entities, with correct name fields
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT * FROM (
                            SELECT 
                                UserID AS EntityId,
                                'User' AS EntityType,
                                (ISNULL(FirstName, '') + ' ' + ISNULL(LastName, '')) AS Name,
                                1 AS IsCreation,
                                CreatedAt AS ActionDate,
                                CreatedAt AS CreatedDate,
                                CreatedBy,
                                NULL AS ModifiedDate,
                                NULL AS ModifiedBy
                            FROM [User]
                            WHERE (@fromDate IS NULL OR CreatedAt >= @fromDate)
                              AND (@toDate IS NULL OR CreatedAt <= @toDate)
                            UNION ALL
                            SELECT 
                                UserID AS EntityId,
                                'User' AS EntityType,
                                (ISNULL(FirstName, '') + ' ' + ISNULL(LastName, '')) AS Name,
                                0 AS IsCreation,
                                ModifiedAt AS ActionDate,
                                NULL AS CreatedDate,
                                NULL AS CreatedBy,
                                ModifiedAt AS ModifiedDate,
                                ModifiedBy
                            FROM [User]
                            WHERE ModifiedAt IS NOT NULL
                              AND (@fromDate IS NULL OR ModifiedAt >= @fromDate)
                              AND (@toDate IS NULL OR ModifiedAt <= @toDate)
                            UNION ALL
                            SELECT 
                                RoleID AS EntityId,
                                'Role' AS EntityType,
                                RoleName AS Name,
                                1 AS IsCreation,
                                CreatedAt AS ActionDate,
                                CreatedAt AS CreatedDate,
                                CreatedBy,
                                NULL AS ModifiedDate,
                                NULL AS ModifiedBy
                            FROM [Role]
                            WHERE (@fromDate IS NULL OR CreatedAt >= @fromDate)
                              AND (@toDate IS NULL OR CreatedAt <= @toDate)
                            UNION ALL
                            SELECT 
                                RoleID AS EntityId,
                                'Role' AS EntityType,
                                RoleName AS Name,
                                0 AS IsCreation,
                                ModifiedAt AS ActionDate,
                                NULL AS CreatedDate,
                                NULL AS CreatedBy,
                                ModifiedAt AS ModifiedDate,
                                ModifiedBy
                            FROM [Role]
                            WHERE ModifiedAt IS NOT NULL
                              AND (@fromDate IS NULL OR ModifiedAt >= @fromDate)
                              AND (@toDate IS NULL OR ModifiedAt <= @toDate)
                            UNION ALL
                            SELECT 
                                DepartmentID AS EntityId,
                                'Department' AS EntityType,
                                DepartmentName AS Name,
                                1 AS IsCreation,
                                CreatedAt AS ActionDate,
                                CreatedAt AS CreatedDate,
                                CreatedBy,
                                NULL AS ModifiedDate,
                                NULL AS ModifiedBy
                            FROM [Department]
                            WHERE (@fromDate IS NULL OR CreatedAt >= @fromDate)
                              AND (@toDate IS NULL OR CreatedAt <= @toDate)
                            UNION ALL
                            SELECT 
                                DepartmentID AS EntityId,
                                'Department' AS EntityType,
                                DepartmentName AS Name,
                                0 AS IsCreation,
                                ModifiedAt AS ActionDate,
                                NULL AS CreatedDate,
                                NULL AS CreatedBy,
                                ModifiedAt AS ModifiedDate,
                                ModifiedBy
                            FROM [Department]
                            WHERE ModifiedAt IS NOT NULL
                              AND (@fromDate IS NULL OR ModifiedAt >= @fromDate)
                              AND (@toDate IS NULL OR ModifiedAt <= @toDate)
                        ) AS AllLogs
                        ORDER BY ActionDate DESC
                        OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY
                    ";
                    cmd.Parameters.AddWithValue("@fromDate", (object?)fromDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@toDate", (object?)toDate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
                    cmd.Parameters.AddWithValue("@pageSize", pageSize);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            logs.Add(new AdminLogEntryViewModel
                            {
                                EntityType = reader["EntityType"]?.ToString() ?? "",
                                Name = reader["Name"]?.ToString() ?? "",
                                IsCreation = reader["IsCreation"] != DBNull.Value && Convert.ToInt32(reader["IsCreation"]) == 1,
                                CreatedDate = reader["CreatedDate"] != DBNull.Value ? (DateTime?)reader["CreatedDate"] : null,
                                CreatedBy = reader["CreatedBy"]?.ToString(),
                                ModifiedDate = reader["ModifiedDate"] != DBNull.Value ? (DateTime?)reader["ModifiedDate"] : null,
                                ModifiedBy = reader["ModifiedBy"]?.ToString()
                            });
                        }
                    }
                }
            }

            var model = new AdminLogsViewModel
            {
                Logs = logs,
                CurrentPage = page,
                PageSize = pageSize,
                TotalLogs = totalLogs,
                FromDate = fromDate,
                ToDate = toDate
            };

            return View("~/Views/Admin/AdminLogs.cshtml", model);
        }
    }
}
