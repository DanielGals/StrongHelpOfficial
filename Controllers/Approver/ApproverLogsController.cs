using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using StrongHelpOfficial.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StrongHelpOfficial.Controllers.Approver
{
    public class ApproverLogsController : Controller
    {
        private readonly IConfiguration _configuration;

        public ApproverLogsController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index(string filterBy = null, DateTime? filterDate = null, int page = 1)
        {
            var model = new ApproverLogsViewModel
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

            int approverUserId = 0;
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
                        approverUserId = (int)result;
                    }
                }

                string query = @"
                    SELECT 
                        lap.LoanApprovalID as LogID,
                        lap.ApprovedDate AS Timestamp,
                        CASE 
                            WHEN lap.IsApproved = 1 THEN 'Approved'
                            WHEN lap.IsApproved = 0 THEN 'Rejected'
                            ELSE 'In Review'
                        END AS Action,
                        'LOAN-' + RIGHT('0000' + CAST(la.LoanID AS NVARCHAR(10)), 4) AS ApplicationID,
                        CASE 
                            WHEN lap.IsApproved = 1 THEN 'Approved loan application'
                            WHEN lap.IsApproved = 0 THEN 'Rejected loan application: ' + ISNULL(lap.Remarks, '')
                            ELSE 'Application under review'
                        END AS Details,
                        CASE 
                            WHEN lap.IsApproved = 1 THEN 'Approved'
                            WHEN lap.IsApproved = 0 THEN 'Rejected'
                            ELSE 'In Review'
                        END AS Status,
                        ISNULL(u.FirstName, '') + ' ' + ISNULL(u.LastName, '') AS UserName
                    FROM LoanApproval lap
                    INNER JOIN LoanApplication la ON lap.LoanID = la.LoanID
                    INNER JOIN [User] u ON la.UserID = u.UserID
                    WHERE lap.ApproverUserID = @UserId
                    AND lap.IsActive = 1
                ";

                if (!string.IsNullOrEmpty(filterBy))
                {
                    if (filterBy == "Approved")
                        query += " AND lap.IsApproved = 1";
                    else if (filterBy == "Rejected")
                        query += " AND lap.IsApproved = 0";
                    else if (filterBy == "In Review")
                        query += " AND lap.IsApproved IS NULL";
                }

                if (filterDate.HasValue)
                {
                    query += " AND CONVERT(date, lap.ApprovedDate) = @FilterDate";
                }

                query += " ORDER BY lap.ApprovedDate DESC";

                int offset = (page - 1) * model.PageSize;
                query += " OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserId", approverUserId);

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
                                    LogID = reader.GetInt32(reader.GetOrdinal("LogID")),
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
                    FROM LoanApproval lap
                    WHERE lap.ApproverUserID = @UserId
                    AND lap.IsActive = 1
                ";

                if (!string.IsNullOrEmpty(filterBy))
                {
                    if (filterBy == "Approved")
                        countQuery += " AND lap.IsApproved = 1";
                    else if (filterBy == "Rejected")
                        countQuery += " AND lap.IsApproved = 0";
                    else if (filterBy == "In Review")
                        countQuery += " AND lap.IsApproved IS NULL";
                }

                if (filterDate.HasValue)
                {
                    countQuery += " AND CONVERT(date, lap.ApprovedDate) = @FilterDate";
                }

                using (var countCommand = new SqlCommand(countQuery, connection))
                {
                    countCommand.Parameters.AddWithValue("@UserId", approverUserId);

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

                model.AvailableActions = new List<string> { "In Review", "Approved", "Rejected" };
            }

            return View("~/Views/Approvers/ApproversLogs.cshtml", model);
        }
    }
}
