using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using StrongHelpOfficial.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StrongHelpOfficial.Controllers.BenefitsAssistant
{
    public class BenefitsAssistantDashboardController : Controller
    {
        private readonly IConfiguration _configuration;

        public BenefitsAssistantDashboardController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            var email = HttpContext.Session.GetString("Email");
            var model = new BenefitsAssistantDashboardViewModel();
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(email))
            {
                model.UserName = "Unknown User";
                return View("~/Views/BenefitsAssistant/BenefitsAssistantDashboard.cshtml", model);
            }

            int benefitsAssistantUserId = 0;
            string firstName = "";
            string lastName = "";

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();

                using (var cmd = new SqlCommand("SELECT UserID, FirstName, LastName FROM [User] WHERE Email = @Email AND IsActive = 1", conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            benefitsAssistantUserId = reader.GetInt32(0);
                            firstName = reader["FirstName"].ToString() ?? "";
                            lastName = reader["LastName"].ToString() ?? "";
                            model.UserName = firstName;
                            HttpContext.Session.SetString("FirstName", firstName);
                            HttpContext.Session.SetString("LastName", lastName);
                        }
                        else
                        {
                            model.UserName = "Unknown User";
                            return View("~/Views/BenefitsAssistant/BenefitsAssistantDashboard.cshtml", model);
                        }
                    }
                }

                // Get stats
                using (var cmd = new SqlCommand(@"
                    SELECT 
                        COUNT(DISTINCT la.LoanID) AS TotalApplications,
                        COUNT(DISTINCT CASE WHEN ApplicationStatus = 'Submitted' THEN la.LoanID END) AS PendingReview,
                        COUNT(DISTINCT CASE WHEN ApplicationStatus IN ('In Review', 'Pending', 'In Progress') THEN la.LoanID END) AS InProgress,
                        COUNT(DISTINCT CASE WHEN ApplicationStatus = 'Approved' THEN la.LoanID END) AS TotalApproved,
                        COUNT(DISTINCT CASE WHEN ApplicationStatus = 'Rejected' THEN la.LoanID END) AS TotalRejected
                    FROM LoanApplication la
                    WHERE (la.BenefitsAssistantUserID = @UserId OR la.BenefitsAssistantUserID IS NULL)
                      AND la.UserID != @UserId
                      AND (la.IsActive = 1 OR la.ApplicationStatus = 'Rejected')
                ", conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", benefitsAssistantUserId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            model.TotalApplications = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                            model.PendingReview = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                            model.InProgress = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                            model.TotalApproved = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
                            model.TotalRejected = reader.IsDBNull(4) ? 0 : reader.GetInt32(4);
                        }
                    }
                }

                // Get pending applications
                var pendingApps = new List<PendingApplicationViewModel>();
                using (var cmd = new SqlCommand(@"
                    SELECT la.LoanID, u.FirstName, u.LastName, la.Title, la.LoanAmount, la.DateSubmitted, la.ApplicationStatus
                    FROM LoanApplication la
                    INNER JOIN [User] u ON la.UserID = u.UserID
                    WHERE (la.BenefitsAssistantUserID = @UserId OR la.BenefitsAssistantUserID IS NULL)
                      AND la.UserID != @UserId
                      AND la.ApplicationStatus = 'Submitted'
                      AND la.IsActive = 1
                    ORDER BY la.DateSubmitted DESC
                ", conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", benefitsAssistantUserId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var empFirstName = reader["FirstName"].ToString() ?? "";
                            var empLastName = reader["LastName"].ToString() ?? "";
                            var initials = (empFirstName.Length > 0 ? empFirstName[0].ToString() : "") +
                                           (empLastName.Length > 0 ? empLastName[0].ToString() : "");

                            pendingApps.Add(new PendingApplicationViewModel
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
            return View("~/Views/BenefitsAssistant/BenefitsAssistantDashboard.cshtml", model);
        }
    }
}