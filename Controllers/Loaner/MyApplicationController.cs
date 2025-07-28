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

                // Get loan application details including Benefits Assistant info
                var loanCmd = new SqlCommand(@"
            SELECT la.*, 
                   u.FirstName, u.LastName,
                   ba.UserID AS BenefitAssistantUserID,
                   ba.FirstName + ' ' + ba.LastName AS BenefitAssistantName,
                   la.DateAssigned,
                   la.Remarks
            FROM LoanApplication la
            JOIN [User] u ON la.UserID = u.UserID
            LEFT JOIN [User] ba ON la.BenefitAssistantUserID = ba.UserID
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
                        model.LoanAmount = (decimal)reader["LoanAmount"];
                        model.BenefitAssistantUserID = reader["BenefitAssistantUserID"] as int?;
                        model.BenefitAssistantName = reader["BenefitAssistantName"] as string;
                        model.DateAssigned = reader["DateAssigned"] as DateTime?;
                        model.Remarks = reader["Remarks"] as string;
                    }
                }

                // Get approval history - ONLY get approved/rejected entries
                var approvalCmd = new SqlCommand(@"
            SELECT 
                la.LoanApprovalID,  
                la.ApproverUserID,
                u.FirstName + ' ' + u.LastName AS ApproverName,
                r.RoleName,
                la.ApprovedDate,
                la.Status,
                la.Comment,  
                la.[Order]
            FROM LoanApproval la
            JOIN [User] u ON la.ApproverUserID = u.UserID
            JOIN Role r ON u.RoleID = r.RoleID
            WHERE la.LoanID = @LoanID
            AND (la.Status = 'Approved' OR la.Status = 'Rejected' OR la.Status = 'Reviewed')
            ORDER BY la.[Order]", conn);


                approvalCmd.Parameters.AddWithValue("@LoanID", loanId);

                model.ApprovalHistory = new List<ApprovalViewModel>();
                using (var reader = await approvalCmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        model.ApprovalHistory.Add(new ApprovalViewModel
                        {
                            ApproverUserID = (int)reader["ApproverUserID"],
                            ApproverName = reader["ApproverName"].ToString(),
                            RoleName = reader["RoleName"].ToString(),
                            ApprovedDate = reader["ApprovedDate"] as DateTime?,
                            Status = reader["Status"].ToString(),
                            Comment = reader["Comment"] as string,
                            Order = (int)reader["Order"]
                        });
                    }
                }

                // Add Benefits Assistant review if application is beyond Submitted
                // and no Benefits Assistant entry exists in ApprovalHistory yet
                bool hasBenefitsAssistantInHistory = model.ApprovalHistory.Any(a =>
                    a.RoleName.Contains("Benefits Assistant"));

                if (model.ApplicationStatus != "Submitted" &&
                    model.ApplicationStatus != "Draft" &&
                    model.BenefitAssistantUserID.HasValue &&
                    !string.IsNullOrEmpty(model.BenefitAssistantName) &&
                    !hasBenefitsAssistantInHistory)
                {
                    model.ApprovalHistory.Add(new ApprovalViewModel
                    {
                        ApproverUserID = model.BenefitAssistantUserID.Value,
                        ApproverName = model.BenefitAssistantName,
                        RoleName = "Benefits Assistant",
                        ApprovedDate = model.DateAssigned,
                        Status = "Reviewed",
                        Comment = model.Remarks ?? "Application reviewed",
                        Order = 0 // Always first in sequence
                    });
                }
            }

            // Sort approvals by order
            model.ApprovalHistory = model.ApprovalHistory.OrderByDescending(a => a.Order).ToList();

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
        [HttpGet]
        [HttpGet]
        [HttpGet]
        public async Task<IActionResult> GetApprovers(int loanId)
        {
            var approvers = new List<object>();

            using (var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                await conn.OpenAsync();

                // First get the application status
                string applicationStatus = "";
                using (var statusCmd = new SqlCommand(@"
            SELECT ApplicationStatus, BenefitAssistantUserID 
            FROM LoanApplication 
            WHERE LoanID = @LoanID", conn))
                {
                    statusCmd.Parameters.AddWithValue("@LoanID", loanId);
                    using (var reader = await statusCmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            applicationStatus = reader["ApplicationStatus"] as string ?? "Submitted";
                        }
                    }
                }

                // Get all approvers including Benefits Assistant
                var cmd = new SqlCommand(@"
            SELECT 
                la.ApproverUserID,
                u.FirstName + ' ' + u.LastName AS ApproverName,
                r.RoleName,
                la.[Order],
                la.Status,
                la.Comment
            FROM LoanApproval la
            JOIN [User] u ON la.ApproverUserID = u.UserID
            JOIN Role r ON u.RoleID = r.RoleID
            WHERE la.LoanID = @LoanID
            ORDER BY la.[Order]", conn);

                cmd.Parameters.AddWithValue("@LoanID", loanId);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        approvers.Add(new
                        {
                            userId = (int)reader["ApproverUserID"],
                            userName = reader["ApproverName"].ToString(),
                            roleName = reader["RoleName"].ToString(),
                            order = (int)reader["Order"],
                            status = reader["Status"].ToString(),
                            description = reader["Comment"] as string ?? ""
                        });
                    }
                }

                // If no Benefits Assistant was found in the approval records but the application 
                // has been reviewed (status "In Review" or beyond), add Benefits Assistant manually
                bool hasBenefitsAssistant = approvers.Any(a => a.ToString().Contains("Benefits Assistant"));

                if (!hasBenefitsAssistant &&
                    (applicationStatus == "In Review" || applicationStatus == "Approved" || applicationStatus == "Rejected"))
                {
                    // Get Benefits Assistant info from LoanApplication
                    using (var baCmd = new SqlCommand(@"
                SELECT 
                    la.BenefitAssistantUserID,
                    u.FirstName + ' ' + u.LastName AS BenefitAssistantName,
                    'Benefits Assistant' AS RoleName,
                    la.Remarks,
                    la.DateAssigned
                FROM LoanApplication la
                LEFT JOIN [User] u ON la.BenefitAssistantUserID = u.UserID
                WHERE la.LoanID = @LoanID
                AND la.BenefitAssistantUserID IS NOT NULL", conn))
                    {
                        baCmd.Parameters.AddWithValue("@LoanID", loanId);

                        using (var reader = await baCmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                approvers.Add(new
                                {
                                    userId = (int)reader["BenefitAssistantUserID"],
                                    userName = reader["BenefitAssistantName"].ToString(),
                                    roleName = "Benefits Assistant",
                                    order = 0, // Always first in sequence
                                    status = "Reviewed",
                                    description = reader["Remarks"] as string ?? "Application reviewed"
                                });
                            }
                        }
                    }
                }
            }

            return Json(approvers);
        }
    }
}
