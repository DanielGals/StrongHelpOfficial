using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using StrongHelpOfficial.Models;

namespace StrongHelpOfficial.Controllers.Loaner
{
    [Area("Loaner")]
    public class LoanerLoanHistoryController : Controller
    {
        private readonly IConfiguration _config;

        public LoanerLoanHistoryController(IConfiguration config)
        {
            _config = config;
        }

        public IActionResult Index(string? status)
        {
            ViewData["UserID"] = HttpContext.Session.GetInt32("UserID");
            ViewData["RoleName"] = HttpContext.Session.GetString("RoleName");
            ViewData["Email"] = HttpContext.Session.GetString("Email");

            var statuses = new List<string> { "Submitted", "In Review", "Approved", "Rejected" };
            var userId = HttpContext.Session.GetInt32("UserID") ?? 0;
            var applications = new List<MyApplicationViewModel>();

            using (var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                conn.Open();
                var sql = @"
                    SELECT LoanID, LoanAmount, DateSubmitted, IsActive, BenefitsAssistantUserID, DateAssigned, ApplicationStatus, Remarks, Title, Description
                    FROM LoanApplication
                    WHERE UserID = @UserID
                    ORDER BY DateSubmitted DESC";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var appStatus = reader["ApplicationStatus"] as string ?? "Submitted";
                            applications.Add(new MyApplicationViewModel
                            {
                                LoanID = (int)reader["LoanID"],
                                LoanAmount = (decimal)reader["LoanAmount"],
                                DateSubmitted = (DateTime)reader["DateSubmitted"],
                                IsActive = (bool)reader["IsActive"],
                                BenefitAssistantUserID = reader["BenefitsAssistantUserID"] as int?,
                                DateAssigned = reader["DateAssigned"] as DateTime?,
                                ApplicationStatus = appStatus,
                                Remarks = reader["Remarks"] as string,
                                Title = reader["Title"] as string,
                                Description = reader["Description"] as string,
                                ProgressPercent = GetProgressPercent(appStatus)
                            });
                        }
                    }
                }
            }

            var model = new LoanerLoanHistoryViewModel
            {
                Applications = applications,
                Statuses = statuses,
                SelectedStatus = status
            };

            return View("~/Views/Loaner/LoanerLoanHistory.cshtml", model);
        }

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

        public IActionResult Details(int id)
        {
            LoanApplicationDetailsViewModel loan = null;

            using (var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                conn.Open();
                var sql = @"
                    SELECT la.LoanID, la.LoanAmount, la.DateSubmitted, la.ApplicationStatus,
                           u.FirstName, u.LastName
                    FROM LoanApplication la
                    JOIN [User] u ON la.UserID = u.UserID
                    WHERE la.LoanID = @LoanID";
                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@LoanID", id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            loan = new LoanApplicationDetailsViewModel
                            {
                                LoanID = (int)reader["LoanID"],
                                LoanAmount = (decimal)reader["LoanAmount"],
                                DateSubmitted = (DateTime)reader["DateSubmitted"],
                                ApplicationStatus = reader["ApplicationStatus"] as string,
                                EmployeeName = $"{reader["FirstName"]} {reader["LastName"]}",
                                Documents = new List<DocumentViewModel>()
                            };
                        }
                    }
                }

                // Fetch documents for this loan
                if (loan != null)
                {
                    var docSql = @"SELECT LoanDocumentID, LoanDocumentName
                                    FROM LoanDocument
                                    WHERE LoanID = @LoanID AND IsActive = 1";
                    using (var docCmd = new SqlCommand(docSql, conn))
                    {
                        docCmd.Parameters.AddWithValue("@LoanID", id);
                        using (var docReader = docCmd.ExecuteReader())
                        {
                            while (docReader.Read())
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

                                loan.Documents.Add(new DocumentViewModel
                                {
                                    LoanDocumentID = (int)docReader["LoanDocumentID"],
                                    Name = name,
                                    Type = type
                                });
                            }
                        }
                    }
                }
            }

            if (loan == null)
                return NotFound();

            return View("~/Views/Loaner/LoanApplicationDetails.cshtml", loan);
        }
    }
}