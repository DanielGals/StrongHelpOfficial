using Microsoft.AspNetCore.Mvc;
using StrongHelpOfficial.Models;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;

namespace StrongHelpOfficial.Controllers.Loaner
{
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
            /* Uncomment this block if you want to save files to the server
            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    var filePath = Path.Combine(uploadsFolder, file.FileName);
                    using var stream = new FileStream(filePath, FileMode.Create);
                    await file.CopyToAsync(stream);
                }
            }
            */
            // Set model properties
            //model.LoanDocumentName = files.Count > 0 ? files.Select(f => f.FileName).ToArray() : null;
            if (files != null && loanAmount != null)
            {
                if (files.Count >= 3 && loanAmount != 0)
                {
                    model.Filecontent = new byte[files.Sum(f => f.Length)];
                    model.LoanDocumentName = new string[files.Count];
                    int offset = 0;
                    int nameIndex = 0;
                    foreach (var file in files)
                    {
                        using var memoryStream = new MemoryStream();
                        await file.CopyToAsync(memoryStream);
                        var fileBytes = memoryStream.ToArray();
                        Array.Copy(fileBytes, 0, model.Filecontent, offset, fileBytes.Length);
                        offset += fileBytes.Length;

                        model.LoanDocumentName[nameIndex++] = file.FileName;
                    }
                    using (var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
                    {
                        await conn.OpenAsync();
                        var userId = HttpContext.Session.GetInt32("UserID") ?? 0;
                        using (var cmd = new SqlCommand("INSERT INTO LoanApplication (LoanAmount, DateSubmitted, UserID) VALUES (@LoanAmount, @DateSubmitted, @UserID)", conn))
                        {
                            cmd.Parameters.AddWithValue("@LoanAmount", model.LoanAmount);
                            cmd.Parameters.AddWithValue("@DateSubmitted", DateTime.Now);
                            // Get UserID from session or set to 0 if not found
                            cmd.Parameters.AddWithValue("@UserID", userId);
                            await cmd.ExecuteNonQueryAsync();
                        }
                        for (int i = 0; i < model.LoanDocumentName.Length; i++)
                        {
                            // Insert each document into the LoanDocument table
                            using (var cmd = new SqlCommand("INSERT INTO LoanDocument (LoanID, FileContent, LoanDocumentName) VALUES ((SELECT LoanID FROM LoanApplication WHERE UserID = @UserID), @FileContent, @LoanDocumentName)", conn))
                            {
                                cmd.Parameters.AddWithValue("@UserID", userId);
                                cmd.Parameters.AddWithValue("@FileContent", model.Filecontent[i]);
                                cmd.Parameters.AddWithValue("@LoanDocumentName", model.LoanDocumentName[i]);
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
            /*
            public async Task<IActionResult> UploadDocuments(List<IFormFile> files)
            {
                var model = new ApplyForLoanViewModel();
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                foreach (var file in files)
                {
                    if (file.Length > 0)
                    {
                        var filePath = Path.Combine(uploadsFolder, file.FileName);
                        using var stream = new FileStream(filePath, FileMode.Create);
                        await file.CopyToAsync(stream);
                    }
                }
                model.LoanAmount = Convert.ToInt32(HttpContext.Request.Form["LoanAmount"]);
                model.LoanDocumentName = HttpContext.Request.Form["LoanDocumentName"];
                // Assuming Filecontent is a byte array, you might want to read the file content here
                // For example, if you want to read the first file's content:
                if (files.Count > 0 && files[0].Length > 0)
                {
                    using var memoryStream = new MemoryStream();
                    await files[0].CopyToAsync(memoryStream);
                    model.Filecontent = memoryStream.ToArray();
                }
                else
                {
                    model.Filecontent = Array.Empty<byte>();
                }
                //model.Filecontent = new byte[Convert.ToInt32(HttpContext.Request.Form["Filecontent"])];
                // Insert LoanAmount into database
                /*using (var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();
                    using (var cmd = new SqlCommand("INSERT INTO LoanApplication (LoanAmount, DateSubmitted, UserID) VALUES (@LoanAmount, @DateSubmitted, @UserID)", conn))
                    {
                        cmd.Parameters.AddWithValue("@LoanAmount", model.LoanAmount);
                        cmd.Parameters.AddWithValue("@DateSubmitted", DateTime.Now);
                        // Get UserID from session or set to 0 if not found
                        var userId = HttpContext.Session.GetInt32("UserID") ?? 0;
                        cmd.Parameters.AddWithValue("@UserID", userId);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                testLang(model);
                return RedirectToAction("Index", "ApplyForLoan");
            }*/
        }
    }

