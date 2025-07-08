using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using StrongHelpOfficial.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StrongHelpOfficial.Controllers.BenefitsAssistant
{
    public class BenefitsAssistantApplicationDetailsController : Controller
    {
        private readonly IConfiguration _config;

        public BenefitsAssistantApplicationDetailsController(IConfiguration config)
        {
            _config = config;
        }

        public async Task<IActionResult> Index(int id)
        {
            var model = await GetApplicationDetailsAsync(id);
            if (model == null)
                return NotFound();

            return View("~/Views/BenefitsAssistant/BenefitsAssistantApplicationDetails.cshtml", model);
        }

        public async Task<IActionResult> ApprovalFlow(int id)
        {
            var model = await GetApplicationDetailsAsync(id);
            if (model == null)
                return NotFound();

            // Use the BenefitsAssistant-specific ApprovalFlow view
            return View("~/Views/BenefitsAssistant/ApprovalFlow.cshtml", model);
        }

        public async Task<IActionResult> ApprovalHistory(int id)
        {
            var model = await GetApplicationDetailsAsync(id);
            if (model == null)
                return NotFound();

            // Use the BenefitsAssistant-specific ApprovalHistory view
            return View("~/Views/BenefitsAssistant/ApprovalHistory.cshtml", model);
        }

        // Helper method to avoid code duplication
        private async Task<BenefitsAssistantApplicationDetailsViewModel> GetApplicationDetailsAsync(int id)
        {
            BenefitsAssistantApplicationDetailsViewModel model = null;
            using (var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                await conn.OpenAsync();
                // Fetch main application details
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
                            model = new BenefitsAssistantApplicationDetailsViewModel
                            {
                                LoanID = reader.GetInt32(reader.GetOrdinal("LoanID")),
                                LoanAmount = reader.GetDecimal(reader.GetOrdinal("LoanAmount")),
                                DateSubmitted = reader.GetDateTime(reader.GetOrdinal("DateSubmitted")),
                                ApplicationStatus = reader["ApplicationStatus"]?.ToString(),
                                EmployeeName = $"{reader["FirstName"]} {reader["LastName"]}",
                                Department = "IT Department", // Placeholder
                                PayrollAccountNumber = "Credit Proceeds to Account Number", // Placeholder
                                Documents = new List<BADocumentViewModel>()
                            };
                        }
                    }
                }
                if (model == null)
                    return null;

                // Fetch documents
                using (var cmd = new SqlCommand(@"
                    SELECT LoanDocumentID, LoanDocumentName, '' AS [Type]
                    FROM LoanDocument
                    WHERE LoanID = @LoanID
                ", conn))
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

                            model.Documents.Add(new BADocumentViewModel
                            {
                                LoanDocumentID = reader.GetInt32(reader.GetOrdinal("LoanDocumentID")),
                                Name = name,
                                Type = type
                            });
                        }
                    }
                }
            }
            return model;
        }
    }
}
