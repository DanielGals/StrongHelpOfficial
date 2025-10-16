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

                string query;
                
                if (!string.IsNullOrEmpty(filterBy))
                {
                    if (filterBy == "Loan Finished")
                    {
                        query = @"
                            SELECT 
                                la.LoanID,
                                la.ModifiedAt AS Timestamp,
                                'Loan Finished' AS Action,
                                'LOAN-' + RIGHT('0000' + CAST(la.LoanID AS NVARCHAR(10)), 4) AS ApplicationID,
                                'Marked loan as finished' AS Details,
                                'Completed' AS Status,
                                ISNULL(u.FirstName, '') + ' ' + ISNULL(u.LastName, '') AS UserName
                            FROM LoanApplication la
                            INNER JOIN [User] u ON la.UserID = u.UserID
                            WHERE la.BenefitsAssistantUserID = @UserId
                            AND la.IsActive = 0
                            AND la.ApplicationStatus = 'Approved'
                            AND la.UserID != @UserId
                            AND la.ModifiedAt IS NOT NULL";
                        if (filterDate.HasValue)
                        {
                            query += " AND CONVERT(date, la.ModifiedAt) = @FilterDate";
                        }
                    }
                    else
                    {
                        query = @"
                            SELECT 
                                lap.LoanID,
                                lap.ApprovedDate AS Timestamp,
                                lap.Status AS Action,
                                'LOAN-' + RIGHT('0000' + CAST(lap.LoanID AS NVARCHAR(10)), 4) AS ApplicationID,
                                CASE 
                                    WHEN lap.Status = 'Reviewed' THEN 'Reviewed and forwarded application'
                                    WHEN lap.Status = 'Rejected' THEN 'Rejected loan application: ' + ISNULL(lap.Comment, '')
                                    ELSE 'Processed application'
                                END AS Details,
                                la.ApplicationStatus AS Status,
                                ISNULL(u.FirstName, '') + ' ' + ISNULL(u.LastName, '') AS UserName
                            FROM LoanApproval lap
                            INNER JOIN LoanApplication la ON lap.LoanID = la.LoanID
                            INNER JOIN [User] u ON la.UserID = u.UserID
                            WHERE lap.UserID = @UserId
                            AND lap.IsActive = 1
                            AND la.UserID != @UserId
                            AND lap.Status = @FilterBy";
                        if (filterDate.HasValue)
                        {
                            query += " AND CONVERT(date, lap.ApprovedDate) = @FilterDate";
                        }
                    }
                }
                else
                {
                    query = @"
                        SELECT 
                            lap.LoanID,
                            lap.ApprovedDate AS Timestamp,
                            lap.Status AS Action,
                            'LOAN-' + RIGHT('0000' + CAST(lap.LoanID AS NVARCHAR(10)), 4) AS ApplicationID,
                            CASE 
                                WHEN lap.Status = 'Reviewed' THEN 'Reviewed and forwarded application'
                                WHEN lap.Status = 'Rejected' THEN 'Rejected loan application: ' + ISNULL(lap.Comment, '')
                                ELSE 'Processed application'
                            END AS Details,
                            la.ApplicationStatus AS Status,
                            ISNULL(u.FirstName, '') + ' ' + ISNULL(u.LastName, '') AS UserName
                        FROM LoanApproval lap
                        INNER JOIN LoanApplication la ON lap.LoanID = la.LoanID
                        INNER JOIN [User] u ON la.UserID = u.UserID
                        WHERE lap.UserID = @UserId
                        AND lap.IsActive = 1
                        AND la.UserID != @UserId";
                    if (filterDate.HasValue)
                    {
                        query += " AND CONVERT(date, lap.ApprovedDate) = @FilterDate";
                    }
                    
                    query += @"
                        
                        UNION ALL
                        
                        SELECT 
                            la.LoanID,
                            la.ModifiedAt AS Timestamp,
                            'Loan Finished' AS Action,
                            'LOAN-' + RIGHT('0000' + CAST(la.LoanID AS NVARCHAR(10)), 4) AS ApplicationID,
                            'Marked loan as finished' AS Details,
                            'Completed' AS Status,
                            ISNULL(u.FirstName, '') + ' ' + ISNULL(u.LastName, '') AS UserName
                        FROM LoanApplication la
                        INNER JOIN [User] u ON la.UserID = u.UserID
                        WHERE la.BenefitsAssistantUserID = @UserId
                        AND la.IsActive = 0
                        AND la.ApplicationStatus = 'Approved'
                        AND la.UserID != @UserId
                        AND la.ModifiedAt IS NOT NULL";
                    if (filterDate.HasValue)
                    {
                        query += " AND CONVERT(date, la.ModifiedAt) = @FilterDate";
                    }
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
                    SELECT COUNT(*) FROM (
                        SELECT lap.LoanID
                        FROM LoanApproval lap
                        INNER JOIN LoanApplication la ON lap.LoanID = la.LoanID
                        WHERE lap.UserID = @UserId
                        AND lap.IsActive = 1
                        AND la.UserID != @UserId
                        
                        UNION ALL
                        
                        SELECT la.LoanID
                        FROM LoanApplication la
                        WHERE la.BenefitsAssistantUserID = @UserId
                        AND la.IsActive = 0
                        AND la.ApplicationStatus = 'Approved'
                        AND la.UserID != @UserId
                        AND la.ModifiedAt IS NOT NULL
                    ) AS AllLogs
                ";

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

                model.AvailableActions = new List<string> {
                    "Reviewed",
                    "Rejected",
                    "Loan Finished"
                };
            }

            return View("~/Views/BenefitsAssistant/BenefitsAssistantLogs.cshtml", model);
        }
    }
}