using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using StrongHelpOfficial.Models;
using System;
using System.Collections.Generic;

namespace StrongHelpOfficial.Controllers.Approver
{
    public class ApproverDashboardController : Controller
    {
        private readonly IConfiguration _configuration;

        public ApproverDashboardController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            var email = HttpContext.Session.GetString("Email");
            var model = new ApproverDashboardViewModel();
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

                // Get application statistics
                using (var cmd = new SqlCommand(@"
                    SELECT 
                        COUNT(*) AS TotalApplications,
                        SUM(CASE WHEN la.ApplicationStatus IN ('Submitted', 'In Review') THEN 1 ELSE 0 END) AS PendingReview,
                        SUM(CASE WHEN la.ApplicationStatus = 'Approved' AND CAST(lap.ApprovedDate AS DATE) = CAST(GETDATE() AS DATE) THEN 1 ELSE 0 END) AS ApprovedToday,
                        SUM(CASE WHEN la.ApplicationStatus = 'Rejected' AND CAST(lap.ApprovedDate AS DATE) = CAST(GETDATE() AS DATE) THEN 1 ELSE 0 END) AS RejectedToday
                    FROM LoanApplication la
                    LEFT JOIN LoanApproval lap ON la.LoanID = lap.LoanID AND lap.ApproverUserID = @UserId
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

                // Get pending applications for review
                var pendingApps = new List<LoanApplicationViewModel>();
                using (var cmd = new SqlCommand(@"
                    SELECT la.LoanID, u.FirstName, u.LastName, la.Title, la.LoanAmount, la.DateSubmitted, la.ApplicationStatus
                    FROM LoanApplication la
                    INNER JOIN [User] u ON la.UserID = u.UserID
                    LEFT JOIN LoanApproval lap ON la.LoanID = lap.LoanID AND lap.ApproverUserID = @UserId
                    WHERE la.ApplicationStatus IN ('Submitted', 'In Review')
                    AND (lap.LoanApprovalID IS NULL OR lap.Status IS NULL)
                    ORDER BY la.DateSubmitted DESC
                ", conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", approverUserId);
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
