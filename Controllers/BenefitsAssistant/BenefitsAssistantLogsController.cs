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

            // Get the current Benefits Assistant's UserID
            int benefitsAssistantUserId = 0;
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // First get the user ID
                using (var cmd = new SqlCommand("SELECT UserID FROM [User] WHERE Email = @Email", connection))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    var result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        benefitsAssistantUserId = (int)result;
                    }
                }

                // Query to get loan applications with the most accurate timestamps
                string query = @"
                    SELECT 
                        la.LoanID,
                        -- Use DateAssigned for 'In Review' status to better reflect when the review began
                        -- Use the latest ApprovedDate for 'Approved' or 'Rejected' statuses
                        -- Fall back to DateSubmitted if nothing else available
                        CASE 
                            WHEN la.ApplicationStatus = 'In Review' THEN la.DateAssigned
                            WHEN la.ApplicationStatus IN ('Approved', 'Rejected') THEN 
                                ISNULL((SELECT MAX(lap.ApprovedDate) FROM LoanApproval lap WHERE lap.LoanID = la.LoanID), la.DateSubmitted)
                            ELSE la.DateSubmitted
                        END AS Timestamp,
                        ISNULL(la.ApplicationStatus, 'Unknown') AS Action,
                        'LOAN-' + RIGHT('0000' + CAST(la.LoanID AS NVARCHAR(10)), 4) AS ApplicationID,
                        CASE 
                            WHEN la.ApplicationStatus = 'Approved' THEN 'Approved bank salary loan application'
                            WHEN la.ApplicationStatus = 'Rejected' THEN 'Rejected loan application: ' + ISNULL(la.Remarks, '')
                            WHEN la.ApplicationStatus = 'In Review' THEN 'Application under review'
                            WHEN la.ApplicationStatus = 'Submitted' THEN 'New application submitted'
                            ELSE ISNULL(la.Title, 'No title')
                        END AS Details,
                        ISNULL(NULLIF(LTRIM(RTRIM(la.Remarks)), ''), la.ApplicationStatus) AS Status,
                        ISNULL(u.FirstName, '') + ' ' + ISNULL(u.LastName, '') AS UserName
                    FROM LoanApplication la
                    INNER JOIN [User] u ON la.UserID = u.UserID
                    WHERE la.BenefitAssistantUserID = @UserId
                    AND la.ApplicationStatus IN ('In Review', 'Approved', 'Rejected')
                ";

                // Add filters if provided
                if (!string.IsNullOrEmpty(filterBy))
                {
                    query += " AND la.ApplicationStatus = @FilterBy";
                }

                if (filterDate.HasValue)
                {
                    // Modified to filter by our calculated Timestamp
                    query += @" AND CONVERT(date, 
                        CASE 
                            WHEN la.ApplicationStatus = 'In Review' THEN la.DateAssigned
                            WHEN la.ApplicationStatus IN ('Approved', 'Rejected') THEN 
                                ISNULL((SELECT MAX(lap.ApprovedDate) FROM LoanApproval lap WHERE lap.LoanID = la.LoanID), la.DateSubmitted)
                            ELSE la.DateSubmitted
                        END) = @FilterDate";
                }

                query += " ORDER BY Timestamp DESC";

                // Add pagination
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

                // Get total count for pagination - modified to match the main query criteria
                string countQuery = @"
                    SELECT COUNT(*) 
                    FROM LoanApplication la
                    WHERE la.BenefitAssistantUserID = @UserId
                    AND la.ApplicationStatus IN ('In Review', 'Approved', 'Rejected')
                ";

                if (!string.IsNullOrEmpty(filterBy))
                {
                    countQuery += " AND la.ApplicationStatus = @FilterBy";
                }

                if (filterDate.HasValue)
                {
                    // Modified to filter by our calculated Timestamp
                    countQuery += @" AND CONVERT(date, 
                        CASE 
                            WHEN la.ApplicationStatus = 'In Review' THEN la.DateAssigned
                            WHEN la.ApplicationStatus IN ('Approved', 'Rejected') THEN 
                                ISNULL((SELECT MAX(lap.ApprovedDate) FROM LoanApproval lap WHERE lap.LoanID = la.LoanID), la.DateSubmitted)
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

                // Get all distinct statuses for filter dropdown - also limit to the three statuses
                using (var statusCommand = new SqlCommand(
                    @"SELECT DISTINCT ApplicationStatus FROM LoanApplication 
                      WHERE BenefitAssistantUserID = @UserId 
                      AND ApplicationStatus IN ('In Review', 'Approved', 'Rejected')
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
