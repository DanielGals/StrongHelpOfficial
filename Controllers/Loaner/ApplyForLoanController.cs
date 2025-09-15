using Microsoft.AspNetCore.Mvc;
using StrongHelpOfficial.Models;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using Microsoft.Extensions.Caching.Memory;

namespace StrongHelpOfficial.Controllers.Loaner
{
    [Area("Loaner")]
    public class ApplyForLoanController : Controller
    {
        private readonly IConfiguration _config;
        private readonly IMemoryCache _memoryCache;
        private const string RequiredDocumentsCacheKey = "RequiredDocuments"; // Same key as in BenefitsAssistantController

        public ApplyForLoanController(IConfiguration config, IMemoryCache memoryCache)
        {
            _config = config;
            _memoryCache = memoryCache;
        }

        public IActionResult Index()
        {
            var model = new ApplyForLoanViewModel();

            // Get required documents from memory cache
            if (_memoryCache.TryGetValue(RequiredDocumentsCacheKey, out List<string> documents))
            {
                model.RequiredDocuments = documents;
            }
            else
            {
                model.RequiredDocuments = new List<string>
                {
                    "Latest 2 months payslip",
                    "Certificate of Employment",
                    "Valid government-issued ID"
                };
            }

            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            ViewData["UserID"] = HttpContext.Session.GetInt32("UserID");
            ViewData["RoleName"] = HttpContext.Session.GetString("RoleName");
            ViewData["Email"] = HttpContext.Session.GetString("Email");

            var userId = HttpContext.Session.GetInt32("UserID");
            int documentCount = 0;
            bool hasExistingLoan = false;
            int? existingLoanId = null;

            if (userId.HasValue)
            {
                try
                {
                    conn.Open();
                    // Check for existing loan
                    var cmdCheck = new SqlCommand("SELECT TOP 1 LoanID FROM LoanApplication WHERE UserID = @UserID", conn);
                    cmdCheck.Parameters.AddWithValue("@UserID", userId);
                    var loanIdObj = cmdCheck.ExecuteScalar();
                    if (loanIdObj != null)
                    {
                        hasExistingLoan = true;
                        existingLoanId = Convert.ToInt32(loanIdObj);
                    }

                    // Calculate document count (optional, keep as is)
                    if (existingLoanId.HasValue)
                    {
                        var cmdCount = new SqlCommand("SELECT COUNT(*) FROM LoanDocument WHERE LoanID = @LoanID AND IsActive = 1", conn);
                        cmdCount.Parameters.AddWithValue("@LoanID", existingLoanId.Value);
                        documentCount = (int)cmdCount.ExecuteScalar();
                    }
                }
                catch
                {
                    // Ignore errors
                }
            }
            ViewData["DocumentCount"] = documentCount;
            ViewData["HasExistingLoan"] = hasExistingLoan;
            ViewData["ExistingLoanId"] = existingLoanId;

            return View("~/Views/Loaner/ApplyForLoan.cshtml", model);
        }

        public IActionResult submissionResult(ApplyForLoanViewModel model)
        {
            TempData["submitResult"] = "Loan request submitted successfully!";
            TempData["LoanJustSubmitted"] = true;
            return RedirectToAction("Index", "ApplyForLoan");
        }

        public IActionResult failedSubmissionResult(ApplyForLoanViewModel model)
        {
            TempData["failedSubmitResult"] = "Your loan must at least have an amount and 3 required documents";
            return RedirectToAction("Index", "ApplyForLoan");
        }

        [HttpPost]
        public async Task<IActionResult> UploadDocuments(ApplyForLoanViewModel model)
        {
            var files = model.Files;
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            var loanAmount = model.LoanAmount;

            // Co-maker logic removed
            if (files != null && loanAmount != null)
            {
                if (files.Count >= 3 && loanAmount != 0)
                {
                    using (var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
                    {
                        await conn.OpenAsync();
                        var userId = HttpContext.Session.GetInt32("UserID") ?? 0;

                        using (var cmd = new SqlCommand(
                            "INSERT INTO LoanApplication (LoanAmount, DateSubmitted, UserID, ApplicationStatus, Title, IsActive, CreatedAt, CreatedBy) " +
                            "VALUES (@LoanAmount, @DateSubmitted, @UserID, @ApplicationStatus, @Title, 1, @CreatedAt, @CreatedBy)", conn))
                        {
                            cmd.Parameters.AddWithValue("@LoanAmount", model.LoanAmount);
                            cmd.Parameters.AddWithValue("@DateSubmitted", DateTime.Now);
                            cmd.Parameters.AddWithValue("@UserID", userId);
                            cmd.Parameters.AddWithValue("@ApplicationStatus", "Submitted");
                            cmd.Parameters.AddWithValue("@Title", " Bank Salary Loan");
                            cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                            cmd.Parameters.AddWithValue("@CreatedBy", userId.ToString());
                            await cmd.ExecuteNonQueryAsync();
                        }

                        // Get the LoanID of the inserted application
                        int loanId = 0;
                        using (var cmd = new SqlCommand("SELECT TOP 1 LoanID FROM LoanApplication WHERE UserID = @UserID ORDER BY DateSubmitted DESC", conn))
                        {
                            cmd.Parameters.AddWithValue("@UserID", userId);
                            loanId = (int)(await cmd.ExecuteScalarAsync() ?? 0);
                        }

                        // Insert each document (add IsActive and audit fields)
                        foreach (var file in files)
                        {
                            if (file.ContentType != "application/pdf" && !file.FileName.ToLower().EndsWith(".pdf"))
                            {
                                TempData["failedSubmitResult"] = "Only PDF files are allowed for upload.";
                                return RedirectToAction("Index", "ApplyForLoan");
                            }

                            byte[] fileBytes;
                            using (var memoryStream = new MemoryStream())
                            {
                                await file.CopyToAsync(memoryStream);
                                fileBytes = memoryStream.ToArray();
                            }
                            using (var cmd = new SqlCommand("INSERT INTO LoanDocument (LoanID, FileContent, LoanDocumentName, IsActive, CreatedAt, CreatedBy) VALUES (@LoanID, @FileContent, @LoanDocumentName, 1, @CreatedAt, @CreatedBy)", conn))
                            {
                                cmd.Parameters.AddWithValue("@LoanID", loanId);
                                cmd.Parameters.AddWithValue("@FileContent", fileBytes);
                                cmd.Parameters.AddWithValue("@LoanDocumentName", file.FileName);
                                cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                                cmd.Parameters.AddWithValue("@CreatedBy", userId.ToString());
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