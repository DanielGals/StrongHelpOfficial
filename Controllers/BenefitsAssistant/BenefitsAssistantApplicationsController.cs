using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using StrongHelpOfficial.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace StrongHelpOfficial.Controllers.BenefitsAssistant
{
    public class BenefitsAssistantApplicationsController : Controller
    {
        private readonly IConfiguration _config;

        public BenefitsAssistantApplicationsController(IConfiguration config)
        {
            _config = config;
        }

        public async Task<IActionResult> Index(string? tab, string? search, int page = 1)
        {
            // Set SelectedTab for view/tab highlighting
            //Set SelectedTab for tab highlighting
            var model = new BenefitsAssistantApplicationsViewModel
            {
                SelectedTab = string.IsNullOrEmpty(tab) ? "Submitted" : (tab == "All Applications" ? null : tab),
                SearchTerm = search,
                Applications = new List<BenefitsApplicationViewModel>()
            };

            var email = HttpContext.Session.GetString("Email");
            if (string.IsNullOrEmpty(email))
            {
                // Optionally redirect or show error
                // redirect or show error
                return View("~/Views/BenefitsAssistant/BenefitsAssistantApplications.cshtml", model);
            }

            int benefitsAssistantUserId = 0;
            using (var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                await conn.OpenAsync();

                // Get the current Benefits Assistant's UserID
                //Get the current Benefits Assistant's UserID
                using (var cmd = new SqlCommand("SELECT UserID FROM [User] WHERE Email = @Email", conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            benefitsAssistantUserId = reader.GetInt32(0);
                        }
                    }
                }

                // Map tab to actual status in DB
                //Map tab to actual status in DB
                string? statusFilter = null;
                if (string.IsNullOrEmpty(tab))
                {
                    // Default to "Submitted" on first load
                    //Default to "Submitted" on first load
                    statusFilter = "Submitted";
                }
                else if (tab != "All Applications")
                {
                    if (tab == "Departments Approval")
                        statusFilter = "Approved";
                    else if (tab == "In Review")
                        statusFilter = "In Review";
                    else if (tab == "Submitted")
                        statusFilter = "Submitted";
                    else if (tab == "Rejected")
                        statusFilter = "Rejected";
                    else
                        statusFilter = tab;
                }

                // Build SQL query for applications - MODIFIED to include unassigned applications

                var sql = @"
                    SELECT la.LoanID, u.UserID, u.FirstName, u.LastName, la.Title, la.LoanAmount, la.DateSubmitted, la.ApplicationStatus
                    FROM LoanApplication la
                    INNER JOIN [User] u ON la.UserID = u.UserID
                    WHERE (la.BenefitAssistantUserID = @UserId OR la.BenefitAssistantUserID IS NULL)";

                if (!string.IsNullOrEmpty(statusFilter))
                {
                    sql += " AND la.ApplicationStatus = @Tab";
                }

                if (!string.IsNullOrEmpty(search))
                {
                    sql += " AND (u.FirstName LIKE @Search OR u.LastName LIKE @Search)";
                }

                sql += " ORDER BY la.DateSubmitted DESC";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", benefitsAssistantUserId);
                    if (!string.IsNullOrEmpty(statusFilter))
                        cmd.Parameters.AddWithValue("@Tab", statusFilter);
                    if (!string.IsNullOrEmpty(search))
                        cmd.Parameters.AddWithValue("@Search", search + "%");

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var firstName = reader["FirstName"].ToString() ?? "";
                            var lastName = reader["LastName"].ToString() ?? "";
                            var initials = (firstName.Length > 0 ? firstName[0].ToString() : "") +
                                           (lastName.Length > 0 ? lastName[0].ToString() : "");

                            model.Applications.Add(new BenefitsApplicationViewModel
                            {
                                ApplicationId = reader["LoanID"].ToString() ?? "",
                                EmployeeName = $"{firstName} {lastName}",
                                Initials = initials.ToUpper(),
                                LoanType = reader["Title"].ToString() ?? "Bank Salary Loan",
                                Amount = (decimal)reader["LoanAmount"],
                                DateApplied = (DateTime)reader["DateSubmitted"],
                                Status = reader["ApplicationStatus"].ToString() ?? "Pending Review"
                            });
                        }
                    }
                }
            }

            // Pagination logic remains the same

            int pageSize = 5;
            int totalApplications = model.Applications.Count;
            model.TotalApplications = totalApplications;
            model.PageNumber = page;
            model.PageSize = pageSize;
            model.Applications = model.Applications
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return View("~/Views/BenefitsAssistant/BenefitsAssistantApplications.cshtml", model);
        }

        public async Task<IActionResult> Details(int id)
        {
            LoanApplicationDetailsViewModel model = null;

            using (var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                await conn.OpenAsync();

                // Fetch main application details (do NOT select Department or PayrollAccountNumber)
                //Fetch main application details
                using (var cmd = new SqlCommand(@"
                    SELECT la.LoanID, la.LoanAmount, la.DateSubmitted, la.ApplicationStatus,
                           u.FirstName, u.LastName
                    FROM LoanApplication la
                    INNER JOIN [User] u ON la.UserID = u.UserID
                    WHERE la.LoanID = @LoanID
        ", conn))
                {
                    cmd.Parameters.AddWithValue("@LoanID", id);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            model = new LoanApplicationDetailsViewModel
                            {
                                LoanID = reader.GetInt32(reader.GetOrdinal("LoanID")),
                                LoanAmount = reader.GetDecimal(reader.GetOrdinal("LoanAmount")),
                                DateSubmitted = reader.GetDateTime(reader.GetOrdinal("DateSubmitted")),
                                ApplicationStatus = reader["ApplicationStatus"]?.ToString(),
                                EmployeeName = $"{reader["FirstName"]} {reader["LastName"]}",
                                // Set placeholders since these columns do not exist
                                //Set placeholders 
                                Department = "IT Department",
                                PayrollAccountNumber = "Credit Proceeds to Account Number",
                                Documents = new List<DocumentViewModel>()
                            };
                        }
                    }
                }

                if (model == null)
                    return NotFound();

                // Fetch documents
                //Fetch documents
                using (var cmd = new SqlCommand(@"
                    SELECT LoanDocumentID, LoanDocumentName, '' AS [Type]
                    FROM LoanDocument
                    WHERE LoanID = @LoanID", conn))
                {
                    cmd.Parameters.AddWithValue("@LoanID", id);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var name = reader["LoanDocumentName"]?.ToString() ?? "";
                            var ext = System.IO.Path.GetExtension(name).ToLowerInvariant();
                            string type = ext switch
                            {
                                ".pdf" => "PDF Document",
                                ".jpg" or ".jpeg" => "Image",
                                ".png" => "Image",
                                _ => "Document"
                            };

                            model.Documents.Add(new DocumentViewModel
                            {
                                LoanDocumentID = reader.GetInt32(reader.GetOrdinal("LoanDocumentID")),
                                Name = name,
                                Type = type
                            });
                        }
                    }
                }
            }
            return View("~/Views/BenefitsAssistant/BenefitsAssistantApplicationDetails.cshtml", model);
        }
    }
}