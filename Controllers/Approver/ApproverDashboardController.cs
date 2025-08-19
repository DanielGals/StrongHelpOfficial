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

        public IActionResult Index(string searchQuery = "")
        {
            var email = HttpContext.Session.GetString("Email");
            var model = new ApproverDashboardViewModel();
            model.SearchQuery = searchQuery;

            string connectionString = _configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            if (string.IsNullOrEmpty(email))
            {
                model.UserName = "Unknown User";
                return View("~/Views/Approvers/ApproversDashboard.cshtml", model);
            }

            int approverUserId = 0;
            string firstName = "";
            string lastName = "";

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();

                // Get the user's details
                using (var cmd = new SqlCommand(@"SELECT u.UserID, u.FirstName, u.LastName, r.RoleName, r.RoleID 
                                                  FROM [User] u
                                                  INNER JOIN [Role] r ON u.RoleID = r.RoleID
                                                  WHERE u.Email = @Email AND r.RoleID IN (3,4,5,6,7,8,9)", conn))
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

                // Get application statistics based on approver role using simpler query
                using (var cmd = new SqlCommand(@"
                    -- Get total applications
                    SELECT 
                        (SELECT COUNT(*) FROM LoanApplication) AS TotalApplications,
                        (
                            SELECT COUNT(DISTINCT la.LoanID)
                            FROM LoanApplication la
                            INNER JOIN LoanApproval current_lap ON la.LoanID = current_lap.LoanID
                            WHERE la.ApplicationStatus IN ('Submitted', 'In Review')
                            AND current_lap.ApproverUserID = @UserId
                            AND current_lap.Status IS NULL
                            AND NOT EXISTS (
                                SELECT 1
                                FROM LoanApproval prev_lap
                                WHERE prev_lap.LoanID = la.LoanID
                                AND prev_lap.[Order] < current_lap.[Order]
                                AND (prev_lap.Status IS NULL OR prev_lap.Status = 'Pending')
                            )
                        ) AS PendingReview,
                        (
                            SELECT COUNT(*) 
                            FROM LoanApproval 
                            WHERE ApproverUserID = @UserId 
                            AND Status = 'Approved' 
                            AND CAST(ApprovedDate AS DATE) = CAST(GETDATE() AS DATE)
                        ) AS ApprovedToday,
                        (
                            SELECT COUNT(*) 
                            FROM LoanApproval 
                            WHERE ApproverUserID = @UserId 
                            AND Status = 'Rejected' 
                            AND CAST(ApprovedDate AS DATE) = CAST(GETDATE() AS DATE)
                        ) AS RejectedToday
                ", conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", approverUserId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            model.TotalApplications = reader.GetInt32(0);
                            model.PendingReview = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                            model.ApprovedToday = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                            model.RejectedToday = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
                        }
                    }
                }

                // Get pending applications for review based on role and approval order
                var pendingApps = new List<LoanApplicationViewModel>();

                var query = @"
                    SELECT la.LoanID, u.FirstName, u.LastName, la.Title, la.LoanAmount, la.DateSubmitted, la.ApplicationStatus
                    FROM LoanApplication la
                    INNER JOIN [User] u ON la.UserID = u.UserID
                    WHERE la.ApplicationStatus IN ('Submitted', 'In Review')
                    AND EXISTS (
                        -- Check if it's this approver's turn based on the approval order
                        SELECT 1 
                        FROM LoanApproval currentApproval
                        WHERE currentApproval.LoanID = la.LoanID
                        AND currentApproval.[Order] = (
                            -- Get the current order in the approval process
                            SELECT MIN(nextApproval.[Order])
                            FROM LoanApproval nextApproval
                            WHERE nextApproval.LoanID = la.LoanID
                            AND (nextApproval.Status IS NULL OR nextApproval.Status = 'Pending')
                        )
                        AND currentApproval.ApproverUserID = @UserId
                    )
                    -- Ensure all previous approvals in the sequence are completed
                    AND NOT EXISTS (
                        SELECT 1 
                        FROM LoanApproval prevApproval
                        INNER JOIN LoanApproval currentApproval ON currentApproval.LoanID = prevApproval.LoanID
                        WHERE prevApproval.LoanID = la.LoanID
                        AND prevApproval.[Order] < (
                            SELECT MIN(myApproval.[Order])
                            FROM LoanApproval myApproval
                            WHERE myApproval.LoanID = la.LoanID
                            AND myApproval.ApproverUserID = @UserId
                        )
                        AND (prevApproval.Status IS NULL OR prevApproval.Status = 'Pending')
                    )";

                // Add search filter if provided
                if (!string.IsNullOrWhiteSpace(searchQuery))
                {
                    query += @" AND (u.FirstName LIKE @SearchQuery OR u.LastName LIKE @SearchQuery 
                               OR la.Title LIKE @SearchQuery OR CAST(la.LoanID AS VARCHAR) = @SearchQueryExact)";
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

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var empFirstName = reader["FirstName"].ToString() ?? "";
                            var empLastName = reader["LastName"].ToString() ?? "";
                            var initials = (empFirstName.Length > 0 ? empFirstName[0].ToString() : "") +
                                          (empLastName.Length > 0 ? empLastName[0].ToString() : "");

                            pendingApps.Add(new LoanApplicationViewModel
                            {
                                ApplicationId = Convert.ToInt32(reader["LoanID"]),
                                EmployeeName = $"{empFirstName} {empLastName}",
                                Initials = initials.ToUpper(),
                                LoanType = reader["Title"].ToString() ?? "",
                                Amount = Convert.ToDecimal(reader["LoanAmount"]),
                                DateApplied = Convert.ToDateTime(reader["DateSubmitted"]),
                                Status = reader["ApplicationStatus"].ToString() ?? ""
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
