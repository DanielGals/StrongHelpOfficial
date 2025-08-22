using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using StrongHelpOfficial.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StrongHelpOfficial.Controllers.Approver
{
    public class ApproverApplicationsController : Controller
    {
        private readonly IConfiguration _configuration;

        public ApproverApplicationsController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<IActionResult> Index(
            string? tab,
            string? search,
            int page = 1,
            decimal? minAmount = null,
            decimal? maxAmount = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var model = new ApproverApplicationsViewModel
            {
                SelectedTab = string.IsNullOrEmpty(tab) ? "In Review" : tab,
                SearchTerm = search ?? string.Empty,
                Applications = new List<LoanApplicationViewModel>(),
                MinAmount = minAmount,
                MaxAmount = maxAmount,
                StartDate = startDate,
                EndDate = endDate
            };

            var email = HttpContext.Session.GetString("Email");
            if (string.IsNullOrEmpty(email))
            {
                return View("~/Views/Approvers/ApproversApplications.cshtml", model);
            }

            string connectionString = _configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            int approverUserId = 0;

            using (var conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();

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
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            approverUserId = reader.GetInt32(0);
                            model.UserName = reader["FirstName"].ToString() ?? "";
                            model.RoleName = reader["RoleName"].ToString() ?? "";
                            model.RoleID = reader.GetInt32(reader.GetOrdinal("RoleID"));
                            model.DepartmentName = reader["DepartmentName"]?.ToString() ?? "";
                        }
                        else
                        {
                            return View("~/Views/Approvers/ApproversApplications.cshtml", model);
                        }
                    }
                }

                // Set the selected tab for display and filtering
                if (string.IsNullOrEmpty(tab))
                    model.SelectedTab = "Pending Review";
                else
                    model.SelectedTab = tab;

                string? statusFilter = null;
                if (model.SelectedTab == "Pending Review")
                    statusFilter = "In Review";
                else if (model.SelectedTab == "Approved")
                    statusFilter = "Approved";
                else if (model.SelectedTab == "Rejected")
                    statusFilter = "Rejected";
                // All Applications has no status filter

                var query = @"
                    SELECT la.LoanID, u.UserID, u.FirstName, u.LastName, la.Title, la.LoanAmount, la.DateSubmitted, la.ApplicationStatus
                    FROM LoanApplication la
                    INNER JOIN [User] u ON la.UserID = u.UserID
                    WHERE la.IsActive = 1
                    AND u.IsActive = 1";

                // If a status filter is specified, add it to the query
                if (!string.IsNullOrEmpty(statusFilter))
                {
                    query += " AND la.ApplicationStatus = @StatusFilter";
                }

                if (model.SelectedTab == "Pending Review")
                {
                    // For Pending Review applications, use the same approach as in ApproverDashboard
                    query += @"
                    AND EXISTS (
                        -- Check if it's this approver's turn based on the approval order
                        SELECT 1 
                        FROM LoanApproval currentApproval
                        WHERE currentApproval.LoanID = la.LoanID
                        AND currentApproval.IsActive = 1
                        AND currentApproval.[Order] = (
                            -- Get the current order in the approval process
                            SELECT MIN(nextApproval.[Order])
                            FROM LoanApproval nextApproval
                            WHERE nextApproval.LoanID = la.LoanID
                            AND nextApproval.IsActive = 1
                            AND (nextApproval.Status IS NULL OR nextApproval.Status = 'Pending')
                        )
                        AND currentApproval.UserID = @UserId
                    )
                    -- Ensure all previous approvals in the sequence are completed
                    AND NOT EXISTS (
                        SELECT 1 
                        FROM LoanApproval prevApproval
                        INNER JOIN LoanApproval currentApproval ON currentApproval.LoanID = prevApproval.LoanID
                        WHERE prevApproval.LoanID = la.LoanID
                        AND prevApproval.IsActive = 1
                        AND currentApproval.IsActive = 1
                        AND prevApproval.[Order] < (
                            SELECT MIN(myApproval.[Order])
                            FROM LoanApproval myApproval
                            WHERE myApproval.LoanID = la.LoanID
                            AND myApproval.IsActive = 1
                            AND myApproval.UserID = @UserId
                        )
                        AND (prevApproval.Status IS NULL OR prevApproval.Status = 'Pending')
                    )";
                }
                else if (model.SelectedTab == "Approved")
                {
                    // Only show applications this approver has approved
                    query += @"
                    AND EXISTS (
                        SELECT 1
                        FROM LoanApproval myApproval
                        WHERE myApproval.LoanID = la.LoanID
                        AND myApproval.IsActive = 1
                        AND myApproval.UserID = @UserId
                        AND myApproval.Status = 'Approved'
                    )";
                }
                else if (model.SelectedTab == "Rejected")
                {
                    // Only show applications this approver has rejected
                    query += @"
                    AND EXISTS (
                        SELECT 1
                        FROM LoanApproval myApproval
                        WHERE myApproval.LoanID = la.LoanID
                        AND myApproval.IsActive = 1
                        AND myApproval.UserID = @UserId
                        AND myApproval.Status = 'Rejected'
                    )";
                }
                else if (model.SelectedTab == "All Applications")
                {
                    // Show all applications associated with this approver
                    query += @"
                    AND EXISTS (
                        SELECT 1
                        FROM LoanApproval myApproval
                        WHERE myApproval.LoanID = la.LoanID
                        AND myApproval.IsActive = 1
                        AND myApproval.UserID = @UserId
                    )";
                }

                // Add search filter
                if (!string.IsNullOrEmpty(search))
                {
                    query += @" AND (u.FirstName LIKE @Search OR u.LastName LIKE @Search 
                              OR la.Title LIKE @Search OR CAST(la.LoanID AS VARCHAR) = @SearchExact)";
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

                    if (!string.IsNullOrEmpty(statusFilter))
                        cmd.Parameters.AddWithValue("@StatusFilter", statusFilter);

                    if (!string.IsNullOrEmpty(search))
                    {
                        cmd.Parameters.AddWithValue("@Search", "%" + search + "%");
                        cmd.Parameters.AddWithValue("@SearchExact", search);
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

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var firstName = reader["FirstName"].ToString() ?? "";
                            var lastName = reader["LastName"].ToString() ?? "";
                            var initials = (firstName.Length > 0 ? firstName[0].ToString() : "") +
                                          (lastName.Length > 0 ? lastName[0].ToString() : "");

                            model.Applications.Add(new LoanApplicationViewModel
                            {
                                ApplicationId = Convert.ToInt32(reader["LoanID"]),
                                EmployeeName = $"{firstName} {lastName}",
                                Initials = initials.ToUpper(),
                                LoanType = reader["Title"].ToString() ?? "",
                                Amount = Convert.ToDecimal(reader["LoanAmount"]),
                                DateApplied = Convert.ToDateTime(reader["DateSubmitted"]),
                                Status = reader["ApplicationStatus"].ToString() ?? ""
                            });
                        }
                    }
                }
            }

            int pageSize = model.PageSize;
            int totalApplications = model.Applications.Count;
            model.TotalApplications = totalApplications;
            model.PageNumber = page;

            model.Applications = model.Applications
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return View("~/Views/Approvers/ApproversApplications.cshtml", model);
        }
    }
}
