using Microsoft.AspNetCore.Mvc;
using StrongHelpOfficial.Models;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;

namespace StrongHelpOfficial.Controllers.Loaner
{
    [Area("Loaner")]
    public class ApplyForLoanController : Controller
    {

        private readonly IConfiguration _config;
        public ApplyForLoanController(IConfiguration config)
        {
            _config = config;
        }
        public IActionResult Index()
        {
            var model = new ApplyForLoanViewModel();
            model.BenefitsAssistantUserID = 2; // Default Benefits Assistant ID
            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            // Get user data from session
            ViewData["UserID"] = HttpContext.Session.GetInt32("UserID");
            ViewData["RoleName"] = HttpContext.Session.GetString("RoleName");
            ViewData["Email"] = HttpContext.Session.GetString("Email");
            return View("~/Views/Loaner/ApplyForLoan.cshtml", model);

        }
        public IActionResult submissionResult(ApplyForLoanViewModel model)
        {
            TempData["submitResult"] = "Loan request submitted successfully!";
            return RedirectToAction("Index", "ApplyForLoan");
        }
        public IActionResult failedSubmissionResult(ApplyForLoanViewModel model)
        {
            TempData["failedSubmitResult"] = "Your loan must at least have an amount and submitted 3 required documents";
            return RedirectToAction("Index", "ApplyForLoan");
        }


        [HttpPost]
        public async Task<IActionResult> UploadDocuments(ApplyForLoanViewModel model)
        {
            // Get uploaded files from the Request
            var files = model.Files;
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            var loanAmount = model.LoanAmount;

            // Set model properties
            if (files != null && loanAmount != null)
            {
                if (files.Count >= 3 && loanAmount != 0)
                {
                    using (var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
                    {
                        await conn.OpenAsync();
                        var userId = HttpContext.Session.GetInt32("UserID") ?? 0;

                        // Insert LoanApplication - MODIFIED to add ApplicationStatus and BenefitsAssistantUserID
                        using (var cmd = new SqlCommand(
                            "INSERT INTO LoanApplication (LoanAmount, DateSubmitted, UserID, ApplicationStatus, Title, BenefitAssistantUserID, DateAssigned) " +
                            "VALUES (@LoanAmount, @DateSubmitted, @UserID, @ApplicationStatus, @Title, @BenefitAssistantUserID, @DateAssigned)", conn))
                        {
                            cmd.Parameters.AddWithValue("@LoanAmount", model.LoanAmount);
                            cmd.Parameters.AddWithValue("@DateSubmitted", DateTime.Now);
                            cmd.Parameters.AddWithValue("@UserID", userId);
                            cmd.Parameters.AddWithValue("@ApplicationStatus", "Submitted");
                            cmd.Parameters.AddWithValue("@Title", "Salary Loan"); // Default title
                            cmd.Parameters.AddWithValue("@BenefitAssistantUserID", 2); // Set to Benefits Assistant with ID 2
                            cmd.Parameters.AddWithValue("@DateAssigned", DateTime.Now);
                            await cmd.ExecuteNonQueryAsync();
                        }

                        // Get the LoanID of the just-inserted application
                        int loanId = 0;
                        using (var cmd = new SqlCommand("SELECT TOP 1 LoanID FROM LoanApplication WHERE UserID = @UserID ORDER BY DateSubmitted DESC", conn))
                        {
                            cmd.Parameters.AddWithValue("@UserID", userId);
                            loanId = (int)(await cmd.ExecuteScalarAsync() ?? 0);
                        }

                        // Insert each document
                        foreach (var file in files)
                        {
                            byte[] fileBytes;
                            using (var memoryStream = new MemoryStream())
                            {
                                await file.CopyToAsync(memoryStream);
                                fileBytes = memoryStream.ToArray();
                            }
                            using (var cmd = new SqlCommand("INSERT INTO LoanDocument (LoanID, FileContent, LoanDocumentName) VALUES (@LoanID, @FileContent, @LoanDocumentName)", conn))
                            {
                                cmd.Parameters.AddWithValue("@LoanID", loanId);
                                cmd.Parameters.AddWithValue("@FileContent", fileBytes);
                                cmd.Parameters.AddWithValue("@LoanDocumentName", file.FileName);
                                await cmd.ExecuteNonQueryAsync();
                            }
                        }
                    }

                    submissionResult(model);
                }
                else
                {
                    model.Filecontent = Array.Empty<byte>();
                    model.LoanDocumentName = Array.Empty<string>();
                    failedSubmissionResult(model);
                }
            }
            else
            {
                model.Filecontent = Array.Empty<byte>();
                model.LoanDocumentName = Array.Empty<string>();
                failedSubmissionResult(model);
            }

            return RedirectToAction("Index", "ApplyForLoan");
        }
    }
}
