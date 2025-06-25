using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using StrongHelpOfficial.Models;
using System.Reflection;

namespace StrongHelpOfficial.Controllers.Loaner
{
    [Area("Loaner")]
    public class MyApplicationController : Controller
    {
        private readonly IConfiguration _config;

        public MyApplicationController(IConfiguration config)
        {
            _config = config;
        }

        public async Task<IActionResult> Index()
        {
            // Get user data from session
            var userId = HttpContext.Session.GetInt32("UserID");
            ViewData["UserID"] = userId;
            ViewData["RoleName"] = HttpContext.Session.GetString("RoleName");
            ViewData["Email"] = HttpContext.Session.GetString("Email");

            var applications = new List<MyApplicationViewModel>();

            if (userId != null)
            {
                using (var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();
                    var cmd = new SqlCommand(@"
                        SELECT LoanID, LoanAmount, DateSubmitted, IsActive, BenefitAssistantUserID, DateAssigned, ApplicationStatus, Remarks, Title, Description
                        FROM LoanApplication
                        WHERE UserID = @UserID
                        ORDER BY DateSubmitted DESC", conn);
                    cmd.Parameters.AddWithValue("@UserID", userId);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var status = reader["ApplicationStatus"] as string ?? "Submitted";
                            applications.Add(new MyApplicationViewModel
                            {
                                LoanID = (int)reader["LoanID"],
                                LoanAmount = (decimal)reader["LoanAmount"],
                                DateSubmitted = (DateTime)reader["DateSubmitted"],
                                IsActive = (bool)reader["IsActive"],
                                BenefitAssistantUserID = reader["BenefitAssistantUserID"] as int?,
                                DateAssigned = reader["DateAssigned"] as DateTime?,
                                ApplicationStatus = status,
                                Remarks = reader["Remarks"] as string,
                                Title = reader["Title"] as string,
                                Description = reader["Description"] as string,
                                ProgressPercent = GetProgressPercent(status)
                            });
                        }
                    }
                }
            }

            return View("~/Views/Loaner/MyApplication.cshtml", applications);
        }

        // Map status to progress percent for UI
        private int GetProgressPercent(string status)
        {
            return status switch
            {
                "Submitted" => 20,
                "In Review" => 60,
                "Approved" => 100,
                "Rejected" => 100,
                _ => 20
            };
        }

        public async Task<IActionResult> Details(int loanId)
        {
            var model = new LoanApplicationDetailsViewModel();

            using (var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                await conn.OpenAsync();

                // Loan details
                var loanCmd = new SqlCommand(@"
            SELECT la.*, u.FirstName, u.LastName, u.Email, u.ContactNum, u.RoleID
            FROM LoanApplication la
            JOIN [User] u ON la.UserID = u.UserID
            WHERE la.LoanID = @LoanID", conn);
                loanCmd.Parameters.AddWithValue("@LoanID", loanId);

                using (var reader = await loanCmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        model.LoanID = (int)reader["LoanID"];
                        model.LoanAmount = (decimal)reader["LoanAmount"];
                        model.DateSubmitted = (DateTime)reader["DateSubmitted"];
                        model.ApplicationStatus = reader["ApplicationStatus"] as string ?? "";
                        model.EmployeeName = $"{reader["FirstName"]} {reader["LastName"]}";
                        model.Department = "IT Department"; // Replace with actual if available
                        model.PayrollAccountNumber = "Credit Proceeds to Account Number"; // Replace with actual if available
                    }
                }

                // Documents: Now include LoanDocumentID and determine file type
                var docCmd = new SqlCommand(@"
            SELECT LoanDocumentID, LoanDocumentName
            FROM LoanDocument
            WHERE LoanID = @LoanID", conn);
                docCmd.Parameters.AddWithValue("@LoanID", loanId);

                using (var docReader = await docCmd.ExecuteReaderAsync())
                {
                    model.Documents = new List<DocumentViewModel>();
                    while (await docReader.ReadAsync())
                    {
                        var name = docReader["LoanDocumentName"] as string ?? "";
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
                            LoanDocumentID = (int)docReader["LoanDocumentID"],
                            Name = name,
                            Type = type
                        });
                    }
                }
            }

            return View("~/Views/Loaner/LoanApplicationDetails.cshtml", model);
        }
        public async Task<IActionResult> ApprovalFlow(int loanId)
        {
            var model = new LoanApplicationDetailsViewModel();

            using (var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                await conn.OpenAsync();

                // Loan details
                var loanCmd = new SqlCommand(@"
            SELECT la.*, u.FirstName, u.LastName
            FROM LoanApplication la
            JOIN [User] u ON la.UserID = u.UserID
            WHERE la.LoanID = @LoanID", conn);
                loanCmd.Parameters.AddWithValue("@LoanID", loanId);

                using (var reader = await loanCmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        model.LoanID = (int)reader["LoanID"];
                        model.ApplicationStatus = reader["ApplicationStatus"] as string ?? "Submitted";
                        // Set other properties as needed
                    }
                }
            }

            return View("~/Views/Loaner/ApprovalFlow.cshtml", model);
        }

        public async Task<IActionResult> ApprovalHistory(int loanId)
        {
            var model = new LoanApplicationDetailsViewModel();

            using (var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                await conn.OpenAsync();

                var loanCmd = new SqlCommand(@"
            SELECT la.*, u.FirstName, u.LastName
            FROM LoanApplication la
            JOIN [User] u ON la.UserID = u.UserID
            WHERE la.LoanID = @LoanID", conn);
                loanCmd.Parameters.AddWithValue("@LoanID", loanId);

                using (var reader = await loanCmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        model.LoanID = (int)reader["LoanID"];
                        model.ApplicationStatus = reader["ApplicationStatus"] as string ?? "Submitted";
                        model.EmployeeName = $"{reader["FirstName"]} {reader["LastName"]}";
                        model.DateSubmitted = (DateTime)reader["DateSubmitted"];
                    }
                }
            }

            return View("~/Views/Loaner/ApprovalHistory.cshtml", model);
        }

        public async Task<IActionResult> DeleteSubmission(int loanId)
        {
            var userId = HttpContext.Session.GetInt32("UserID");

            using (var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand(@"
                    DELETE FROM LoanDocument WHERE LoanID = @LoanID;
                    DELETE FROM LoanApplication WHERE LoanID = @LoanID;
                ", conn);
                cmd.Parameters.AddWithValue("@LoanID", loanId);

                await cmd.ExecuteNonQueryAsync();
            }

            TempData["SuccessMessage"] = "Your application was successfully deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
