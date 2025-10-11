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
            var email = HttpContext.Session.GetString("Email");
            
            // Fetch FirstName from database like LoanerDashboard does
            string firstName = "Unknown User";
            if (!string.IsNullOrEmpty(email))
            {
                try
                {
                    conn.Open();
                    using (var cmd = new SqlCommand("SELECT FirstName FROM [User] WHERE Email = @Email AND IsActive = 1", conn))
                    {
                        cmd.Parameters.AddWithValue("@Email", email);
                        var result = cmd.ExecuteScalar();
                        if (result != null)
                        {
                            firstName = result.ToString();
                        }
                    }
                }
                catch
                {
                    // Keep default value on error
                }
            }
            ViewData["FirstName"] = firstName;
            int documentCount = 0;
            bool hasExistingLoan = false;
            int? existingLoanId = null;

            if (userId.HasValue)
            {
                try
                {
                    if (conn.State != System.Data.ConnectionState.Open)
                        conn.Open();
                    // Check for existing active loan
                    var cmdCheck = new SqlCommand("SELECT TOP 1 LoanID FROM LoanApplication WHERE UserID = @UserID AND IsActive = 1", conn);
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

                    if (existingLoanId.HasValue)
                    {
                        // Fetch co-maker ID for the existing loan
                        var cmdCoMaker = new SqlCommand("SELECT ComakerUserId FROM LoanApplication WHERE LoanID = @LoanID", conn);
                        cmdCoMaker.Parameters.AddWithValue("@LoanID", existingLoanId.Value);
                        var coMakerIdObj = cmdCoMaker.ExecuteScalar();
                        if (coMakerIdObj != DBNull.Value && coMakerIdObj != null)
                        {
                            int coMakerId = Convert.ToInt32(coMakerIdObj);
                            // Fetch co-maker's name
                            var cmdCoMakerName = new SqlCommand("SELECT FirstName, LastName FROM [User] WHERE UserID = @UserID", conn);
                            cmdCoMakerName.Parameters.AddWithValue("@UserID", coMakerId);
                            using (var reader = cmdCoMakerName.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    string coMakerName = reader.GetString(0) + " " + reader.GetString(1);
                                    ViewData["CoMakerStatus"] = coMakerName;
                                }
                                else
                                {
                                    ViewData["CoMakerStatus"] = "No assigned co-maker yet";
                                }
                            }
                        }
                        else
                        {
                            ViewData["CoMakerStatus"] = "No assigned co-maker yet";
                        }
                    }
                    else
                    {
                        ViewData["CoMakerStatus"] = "No assigned co-maker yet";
                    }
                }
                catch
                {
                    // Ignore errors
                }
            }
            ViewData["DocumentCount"] = documentCount;
            
            // Only show existing loan message if loan wasn't just submitted
            if (TempData["LoanJustSubmitted"] == null)
            {
                ViewData["HasExistingLoan"] = hasExistingLoan;
            }
            else
            {
                ViewData["HasExistingLoan"] = false;
            }
            
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
            TempData["failedSubmitResult"] = "Your loan must at least have an amount, the 3 required documents, and an assigned co-maker!";
            return RedirectToAction("Index", "ApplyForLoan");
        }

        [HttpPost]
        public async Task<IActionResult> UploadDocuments(ApplyForLoanViewModel model)
        {
            var files = model.Files;
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            var loanAmount = model.LoanAmount;
            var coMakerId = model.CoMakerId;

            // Validate co-maker
            if (coMakerId == null || coMakerId == 0)
            {
                return Json(new { success = false, error = "You must assign a co-maker before submitting your loan application!" });
            }

            // Validate files and loan amount
            if (files != null && loanAmount != null)
            {
                if (files.Count >= 3 && loanAmount != 0)
                {
                    using (var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
                    {
                        await conn.OpenAsync();
                        var userId = HttpContext.Session.GetInt32("UserID") ?? 0;

                        // Fetch user's full name
                        string createdByName = "";
                        using (var nameCmd = new SqlCommand("SELECT FirstName, LastName FROM [User] WHERE UserID = @UserID", conn))
                        {
                            nameCmd.Parameters.AddWithValue("@UserID", userId);
                            using (var reader = await nameCmd.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    createdByName = reader.GetString(0) + " " + reader.GetString(1);
                                }
                            }
                        }

                        using (var cmd = new SqlCommand(
                            "INSERT INTO LoanApplication (LoanAmount, DateSubmitted, UserID, ApplicationStatus, Title, IsActive, CreatedAt, CreatedBy, ComakerUserId) " +
                            "VALUES (@LoanAmount, @DateSubmitted, @UserID, @ApplicationStatus, @Title, 1, @CreatedAt, @CreatedBy, @CoMakerId)", conn))
                        {
                            cmd.Parameters.AddWithValue("@LoanAmount", model.LoanAmount);
                            cmd.Parameters.AddWithValue("@DateSubmitted", DateTime.Now);
                            cmd.Parameters.AddWithValue("@UserID", userId);
                            cmd.Parameters.AddWithValue("@ApplicationStatus", "Drafted");
                            cmd.Parameters.AddWithValue("@Title", "Bank Salary Loan");
                            cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                            cmd.Parameters.AddWithValue("@CreatedBy", createdByName);
                            cmd.Parameters.AddWithValue("@CoMakerId", (object?)coMakerId ?? DBNull.Value);
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
                                return Json(new { success = false, error = "Only PDF files are allowed for upload." });
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

                    // Set success message
                    TempData["submitResult"] = "Loan request submitted successfully!";
                    TempData["LoanJustSubmitted"] = true;

                    // Success: return redirect URL for AJAX
                    return Json(new { success = true, redirectUrl = Url.Action("Index", "ApplyForLoan", new { area = "Loaner" }) });
                }
                else
                {
                    return Json(new { success = false, error = "Your loan must at least have an amount, the 3 required documents, and an assigned co-maker!" });
                }
            }
            else
            {
                return Json(new { success = false, error = "Your loan must at least have an amount, the 3 required documents, and an assigned co-maker!" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> SearchUsers(string term)
        {
            var results = new List<object>();
            var currentUserId = HttpContext.Session.GetInt32("UserID");

            using (var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand(@"
                    SELECT TOP 5 UserID, FirstName, LastName
                    FROM [User]
                    WHERE (FirstName LIKE @term OR LastName LIKE @term)
                        AND UserID <> @currentUserId
                        AND IsActive = 1
                        AND UserID NOT IN (
                            SELECT ISNULL(ComakerUserId, 0)
                            FROM LoanApplication
                            WHERE IsActive = 1 AND ComakerUserId IS NOT NULL
                        )
                    ORDER BY FirstName, LastName", conn);
                cmd.Parameters.AddWithValue("@term", $"%{term}%");
                cmd.Parameters.AddWithValue("@currentUserId", currentUserId ?? 0);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        results.Add(new
                        {
                            userId = reader.GetInt32(0),
                            name = reader.GetString(1) + " " + reader.GetString(2)
                        });
                    }
                }
            }
            return Json(results);
        }
    }
}