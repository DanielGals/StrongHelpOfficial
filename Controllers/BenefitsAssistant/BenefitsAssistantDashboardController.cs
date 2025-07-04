﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using StrongHelpOfficial.Models;
using System;
using System.Collections.Generic;

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
            // Get the logged-in user's email from session
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

                // Get UserID, FirstName, and LastName for the logged-in user
                using (var cmd = new SqlCommand("SELECT UserID, FirstName, LastName FROM [User] WHERE Email = @Email", conn))
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
                            // Update session with correct names
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

                // 1. Get stats
                using (var cmd = new SqlCommand(@"
                    SELECT 
                        COUNT(*) AS TotalApplications,
                        SUM(CASE WHEN ApplicationStatus IN ('Submitted', 'In Review') THEN 1 ELSE 0 END) AS PendingReview,
                        SUM(CASE WHEN ApplicationStatus = 'Approved' AND CAST(DateSubmitted AS DATE) = CAST(GETDATE() AS DATE) THEN 1 ELSE 0 END) AS ApprovedToday,
                        SUM(CASE WHEN ApplicationStatus = 'Rejected' AND CAST(DateSubmitted AS DATE) = CAST(GETDATE() AS DATE) THEN 1 ELSE 0 END) AS RejectedToday
                    FROM LoanApplication
                    WHERE BenefitAssistantUserID = @UserId
                ", conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", benefitsAssistantUserId);
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

                // 2. Get pending applications (with status)
                var pendingApps = new List<PendingApplicationViewModel>();
                using (var cmd = new SqlCommand(@"
                    SELECT la.LoanID, u.FirstName, u.LastName, la.Title, la.LoanAmount, la.DateSubmitted, la.ApplicationStatus
                    FROM LoanApplication la
                    INNER JOIN [User] u ON la.UserID = u.UserID
                    WHERE la.BenefitAssistantUserID = @UserId
                      AND la.ApplicationStatus IN ('Submitted', 'In Review')
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
                // Only take 5
                model.PendingApplications = pendingApps.Take(5).ToList();
            }
            return View("~/Views/BenefitsAssistant/BenefitsAssistantDashboard.cshtml", model);
        }
    }
}
