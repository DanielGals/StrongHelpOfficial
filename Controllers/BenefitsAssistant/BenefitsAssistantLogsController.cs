using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using StrongHelpOfficial.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StrongHelpOfficial.Controllers.BenefitsAssistant
{
    public class BenefitsAssistantLogsController : Controller
    {
        private readonly IConfiguration _configuration;

        public BenefitsAssistantLogsController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index(string filterBy = null, DateTime? filterDate = null, int page = 1)
        {
            var model = new BenefitsAssistantLogsViewModel
            {
                CurrentPage = page,
                PageSize = 5,
                FilterBy = filterBy,
                FilterDate = filterDate
            };

            var email = HttpContext.Session.GetString("Email");
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Login", "Auth");
            }

            int benefitsAssistantUserId = 0;
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (var cmd = new SqlCommand("SELECT UserID FROM [User] WHERE Email = @Email AND IsActive = 1", connection))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    var result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        benefitsAssistantUserId = (int)result;
                    }
                }

                string query = @"
                    SELECT 
                        la.LoanID,
                        CASE 
                            WHEN la.ApplicationStatus = 'In Review' THEN ISNULL(la.DateAssigned, la.DateSubmitted)
                            WHEN la.ApplicationStatus IN ('Approved', 'Rejected') THEN 
                                ISNULL((SELECT MAX(lap.ApprovedDate) FROM LoanApproval lap WHERE lap.LoanID = la.LoanID AND lap.IsActive = 1), la.DateSubmitted)
                            ELSE la.DateSubmitted
                        END AS Timestamp,
                        ISNULL(la.ApplicationStatus, 'Unknown') AS Action,
                        'LOAN-' + RIGHT('0000' + CAST(la.LoanID AS NVARCHAR(10)), 4) AS ApplicationID,
                        CASE 
                            WHEN la.ApplicationStatus = 'Approved' THEN 'Approved bank salary loan application'
                            WHEN la.ApplicationStatus = 'Rejected' THEN 'Rejected loan application: ' + ISNULL(la.Remarks, '')
                            WHEN la.ApplicationStatus = 'In Review' THEN 'Application under review'
                            ELSE ISNULL(la.Title, 'No title')
                        END AS Details,
                        ISNULL(NULLIF(LTRIM(RTRIM(la.Remarks)), ''), la.ApplicationStatus) AS Status,
                        ISNULL(u.FirstName, '') + ' ' + ISNULL(u.LastName, '') AS UserName
                    FROM LoanApplication la
                    INNER JOIN [User] u ON la.UserID = u.UserID
                    WHERE la.BenefitsAssistantUserID = @UserId
                    AND la.ApplicationStatus IN ('In Review', 'Approved', 'Rejected')
                    AND la.IsActive = 1
                ";

                if (!string.IsNullOrEmpty(filterBy))
                {
                    query += " AND la.ApplicationStatus = @FilterBy";
                }

                if (filterDate.HasValue)
                {
                    query += @" AND CONVERT(date, 
                        CASE 
                            WHEN la.ApplicationStatus = 'In Review' THEN la.DateAssigned
                            WHEN la.ApplicationStatus IN ('Approved', 'Rejected') THEN 
                                ISNULL((SELECT MAX(lap.ApprovedDate) FROM LoanApproval lap WHERE lap.LoanID = la.LoanID AND lap.IsActive = 1), la.DateSubmitted)
                            ELSE la.DateSubmitted
                        END) = @FilterDate";
                }

                query += " ORDER BY Timestamp DESC";

                int offset = (page - 1) * model.PageSize;
                query += " OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", benefitsAssistantUserId);

                    if (!string.IsNullOrEmpty(filterBy))
                    {
                        command.Parameters.AddWithValue("@FilterBy", filterBy);
                    }

                    if (filterDate.HasValue)
                    {
                        command.Parameters.AddWithValue("@FilterDate", filterDate.Value.Date);
                    }

                    command.Parameters.AddWithValue("@Offset", offset);
                    command.Parameters.AddWithValue("@PageSize", model.PageSize);

                    try
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            var logs = new List<ActivityLogEntry>();
                            while (reader.Read())
                            {
                                logs.Add(new ActivityLogEntry
                                {
                                    LogID = reader.GetInt32(reader.GetOrdinal("LoanID")),
                                    Timestamp = reader.IsDBNull(reader.GetOrdinal("Timestamp")) ? DateTime.Now : reader.GetDateTime(reader.GetOrdinal("Timestamp")),
                                    Action = reader.IsDBNull(reader.GetOrdinal("Action")) ? "Unknown" : reader.GetString(reader.GetOrdinal("Action")),
                                    ApplicationID = reader.IsDBNull(reader.GetOrdinal("ApplicationID")) ? "Unknown" : reader.GetString(reader.GetOrdinal("ApplicationID")),
                                    Details = reader.IsDBNull(reader.GetOrdinal("Details")) ? "No details available" : reader.GetString(reader.GetOrdinal("Details")),
                                    Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? "Unknown" : reader.GetString(reader.GetOrdinal("Status")),
                                    UserName = reader.IsDBNull(reader.GetOrdinal("UserName")) ? "Unknown User" : reader.GetString(reader.GetOrdinal("UserName"))
                                });
                            }
                            model.Logs = logs;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error reading logs: {ex.Message}");
                        model.Logs = new List<ActivityLogEntry>();
                    }
                }

                string countQuery = @"
                    SELECT COUNT(*) 
                    FROM LoanApplication la
                    WHERE la.BenefitsAssistantUserID = @UserId
                    AND la.ApplicationStatus IN ('In Review', 'Approved', 'Rejected')
                    AND la.IsActive = 1
                ";

                if (!string.IsNullOrEmpty(filterBy))
                {
                    countQuery += " AND la.ApplicationStatus = @FilterBy";
                }

                if (filterDate.HasValue)
                {
                    countQuery += @" AND CONVERT(date, 
                        CASE 
                            WHEN la.ApplicationStatus = 'In Review' THEN la.DateAssigned
                            WHEN la.ApplicationStatus IN ('Approved', 'Rejected') THEN 
                                ISNULL((SELECT MAX(lap.ApprovedDate) FROM LoanApproval lap WHERE lap.LoanID = la.LoanID AND lap.IsActive = 1), la.DateSubmitted)
                            ELSE la.DateSubmitted
                        END) = @FilterDate";
                }

                using (var countCommand = new SqlCommand(countQuery, connection))
                {
                    countCommand.Parameters.AddWithValue("@UserId", benefitsAssistantUserId);

                    if (!string.IsNullOrEmpty(filterBy))
                    {
                        countCommand.Parameters.AddWithValue("@FilterBy", filterBy);
                    }

                    if (filterDate.HasValue)
                    {
                        countCommand.Parameters.AddWithValue("@FilterDate", filterDate.Value.Date);
                    }

                    try
                    {
                        model.TotalItems = (int)countCommand.ExecuteScalar();
                    }
                    catch
                    {
                        model.TotalItems = model.Logs.Count;
                    }

                    model.TotalPages = Math.Max(1, (int)Math.Ceiling((double)model.TotalItems / model.PageSize));
                }

                using (var statusCommand = new SqlCommand(
                    @"SELECT DISTINCT ApplicationStatus FROM LoanApplication 
                      WHERE BenefitsAssistantUserID = @UserId
                      AND ApplicationStatus IN ('In Review', 'Approved', 'Rejected')
                      AND IsActive = 1
                      ORDER BY ApplicationStatus", connection))
                {
                    statusCommand.Parameters.AddWithValue("@UserId", benefitsAssistantUserId);

                    try
                    {
                        var actions = new List<string>();
                        using (var reader = statusCommand.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                actions.Add(reader.GetString(0));
                            }
                        }
                        model.AvailableActions = actions;
                    }
                    catch
                    {
                        model.AvailableActions = new List<string> {
                            "In Review",
                            "Approved",
                            "Rejected"
                        };
                    }
                }
            }

            return View("~/Views/BenefitsAssistant/BenefitsAssistantLogs.cshtml", model);
        }
    }
}