using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using StrongHelpOfficial.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StrongHelpOfficial.Controllers.Approver
{
    public class ApproverDashboardController : Controller
    {
        private readonly IConfiguration _configuration;

        public ApproverDashboardController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index(
            string searchQuery = "",
            decimal? minAmount = null,
            decimal? maxAmount = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var email = HttpContext.Session.GetString("Email");
            var model = new ApproverDashboardViewModel
            {
                SearchQuery = searchQuery,
                MinAmount = minAmount,
                MaxAmount = maxAmount,
                StartDate = startDate,
                EndDate = endDate
            };

            string connectionString = _configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            if (string.IsNullOrEmpty(email))
            {
                model.UserName = "Unknown User";
                return View("~/Views/Approvers/ApproversDashboard.cshtml", model);
            }

            // Log to console for debugging purposes
            Console.WriteLine("Looking up approver with email: " + email);

            int approverUserId = 0;
            string firstName = "";
            string lastName = "";

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();

                // Get the user's details
                using (var cmd = new SqlCommand(@"SELECT u.UserID, u.FirstName, u.LastName, r.RoleName, r.RoleID, 
                                                  d.DepartmentName
                                                  FROM [User] u
                                                  INNER JOIN [Role] r ON u.RoleID = r.RoleID
                                                  LEFT JOIN Department d ON u.DepartmentID = d.DepartmentID
                                                  WHERE u.Email = @Email AND r.RoleID IN (3,4,5,6,7,8,9)
                                                  AND u.IsActive = 1 AND r.IsActive = 1", conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            approverUserId = reader.GetInt32(0);
                            firstName = reader["FirstName"].ToString() ?? "";
                            lastName = reader["LastName"].ToString() ?? "";
                            model.UserName = firstName;
                            model.RoleName = reader["RoleName"].ToString() ?? "";
                            model.RoleID = reader.GetInt32(reader.GetOrdinal("RoleID"));
                            model.DepartmentName = reader["DepartmentName"]?.ToString() ?? "";
                            HttpContext.Session.SetString("FirstName", firstName);
                            HttpContext.Session.SetString("LastName", lastName);
                            HttpContext.Session.SetString("ApproverRoleName", model.RoleName);
                        }
                        else
                        {
                            model.UserName = "Unknown User";
                            return View("~/Views/Approvers/ApproversDashboard.cshtml", model);
                        }
                    }
                }

                // Get application statistics based on approver role
                using (var cmd = new SqlCommand(@"

                        (
                            SELECT COUNT(DISTINCT la.LoanID)
                            FROM LoanApplication la
                            WHERE la.ApplicationStatus IN ('Submitted', 'In Review', 'In Progress')
                            AND la.IsActive = 1

                            )
                        ) AS PendingReview,
                        -- In Progress: Applications this approver has approved but others still need to review
                        (
                            SELECT COUNT(DISTINCT la.LoanID)
                            FROM LoanApplication la
                            WHERE la.ApplicationStatus IN ('Submitted', 'In Review', 'In Progress')
                            AND la.IsActive = 1
                            AND EXISTS (
                                SELECT 1 FROM LoanApproval my_approval
                                WHERE my_approval.LoanID = la.LoanID
                                AND my_approval.UserID = @UserId
                                AND my_approval.Status = 'Approved'
                                AND my_approval.IsActive = 1
                            )
                            AND EXISTS (
                                SELECT 1 FROM LoanApproval pending_approval
                                WHERE pending_approval.LoanID = la.LoanID
                                AND pending_approval.IsActive = 1
                                AND (pending_approval.Status IS NULL OR pending_approval.Status = 'Pending')
                            )
                        ) AS InProgress,
                        -- Completed: Applications approved by all approvers
                        (
                            SELECT COUNT(DISTINCT la.LoanID)
                            FROM LoanApplication la
                            INNER JOIN LoanApproval my_lap ON la.LoanID = my_lap.LoanID
                            WHERE la.IsActive = 1
                            AND my_lap.UserID = @UserId
                            AND my_lap.Status = 'Approved'
                            AND my_lap.IsActive = 1
                            AND NOT EXISTS (
                                SELECT 1 FROM LoanApproval pending_lap
                                WHERE pending_lap.LoanID = la.LoanID
                                AND pending_lap.IsActive = 1
                                AND pending_lap.Status IS NULL
                            )
                        ) AS Completed
                ", conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", approverUserId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            model.PendingReview = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                            model.InProgress = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                            model.Completed = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                        }
                    }
                }

                // Get pending applications for review based on role and approval order
                var pendingApps = new List<LoanApplicationViewModel>();

                var query = @"
                    SELECT la.LoanID, u.FirstName, u.LastName, la.Title, la.LoanAmount, la.DateSubmitted, la.ApplicationStatus
                    FROM LoanApplication la
                    INNER JOIN [User] u ON la.UserID = u.UserID
                    WHERE la.ApplicationStatus IN ('Submitted', 'In Review', 'In Progress')
                    AND la.IsActive = 1
                    AND u.IsActive = 1
                    AND EXISTS (
                        SELECT 1 FROM LoanApproval currentApproval
                        WHERE currentApproval.LoanID = la.LoanID
                        AND currentApproval.UserID = @UserId
                        AND currentApproval.IsActive = 1
                        AND (currentApproval.Status IS NULL OR currentApproval.Status = 'Pending')
                    )
                    -- For In Progress applications, ensure all previous approvers have approved
                    AND (
                        la.ApplicationStatus != 'In Progress'
                        OR NOT EXISTS (
                            SELECT 1 FROM LoanApproval prevApproval
                            INNER JOIN LoanApproval myApproval ON prevApproval.LoanID = myApproval.LoanID
                            WHERE prevApproval.LoanID = la.LoanID
                            AND myApproval.UserID = @UserId
                            AND prevApproval.[Order] < myApproval.[Order]
                            AND prevApproval.IsActive = 1
                            AND myApproval.IsActive = 1
                            AND (prevApproval.Status IS NULL OR prevApproval.Status = 'Pending')
                        )
                    )";

                // Add search filter if provided
                if (!string.IsNullOrWhiteSpace(searchQuery))
                {
                    query += @" AND (u.FirstName LIKE @SearchQuery OR u.LastName LIKE @SearchQuery 
                               OR la.Title LIKE @SearchQuery OR CAST(la.LoanID AS VARCHAR) = @SearchQueryExact)";
                }

                // Add amount range filter if provided
                if (minAmount.HasValue)
                {
                    query += " AND la.LoanAmount >= @MinAmount";
                }

                if (maxAmount.HasValue)
                {
                    query += " AND la.LoanAmount <= @MaxAmount";
                }

                // Add date range filter if provided
                if (startDate.HasValue)
                {
                    query += " AND CAST(la.DateSubmitted AS DATE) >= CAST(@StartDate AS DATE)";
                }

                if (endDate.HasValue)
                {
                    query += " AND CAST(la.DateSubmitted AS DATE) <= CAST(@EndDate AS DATE)";
                }

                query += " ORDER BY la.DateSubmitted DESC";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", approverUserId);

                    if (!string.IsNullOrWhiteSpace(searchQuery))
                    {
                        cmd.Parameters.AddWithValue("@SearchQuery", "%" + searchQuery + "%");
                        cmd.Parameters.AddWithValue("@SearchQueryExact", searchQuery);
                    }

                    // Add amount range parameters
                    if (minAmount.HasValue)
                    {
                        cmd.Parameters.AddWithValue("@MinAmount", minAmount.Value);
                    }

                    if (maxAmount.HasValue)
                    {
                        cmd.Parameters.AddWithValue("@MaxAmount", maxAmount.Value);
                    }

                    // Add date range parameters
                    if (startDate.HasValue)
                    {
                        cmd.Parameters.AddWithValue("@StartDate", startDate.Value);
                    }

                    if (endDate.HasValue)
                    {
                        cmd.Parameters.AddWithValue("@EndDate", endDate.Value);
                    }

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var empFirstName = reader["FirstName"].ToString() ?? "";
                            var empLastName = reader["LastName"].ToString() ?? "";
                            var initials = (empFirstName.Length > 0 ? empFirstName[0].ToString() : "") +
                                          (empLastName.Length > 0 ? empLastName[0].ToString() : "");

                            var dbStatus = reader["ApplicationStatus"].ToString() ?? "";
                            var displayStatus = dbStatus;
                            
                            // Show 'In Review' if it's In Progress but ready for current user
                            if (dbStatus == "In Progress")
                            {
                                displayStatus = "In Review";
                            }
                            
                            pendingApps.Add(new LoanApplicationViewModel
                            {
                                ApplicationId = Convert.ToInt32(reader["LoanID"]),
                                EmployeeName = $"{empFirstName} {empLastName}",
                                Initials = initials.ToUpper(),
                                LoanType = reader["Title"].ToString() ?? "",
                                Amount = Convert.ToDecimal(reader["LoanAmount"]),
                                DateApplied = Convert.ToDateTime(reader["DateSubmitted"]),
                                Status = displayStatus
                            });
                        }
                    }
                }

                model.PendingApplications = pendingApps.Take(5).ToList();
            }

            return View("~/Views/Approvers/ApproversDashboard.cshtml", model);
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Auth");
        }
    }
}
