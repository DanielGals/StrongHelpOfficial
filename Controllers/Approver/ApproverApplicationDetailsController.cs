using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using StrongHelpOfficial.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Linq;
using System.IO;

namespace StrongHelpOfficial.Controllers.Approver
{
    public class ApproverApplicationDetailsController : Controller
    {
        private readonly IConfiguration _config;

        public ApproverApplicationDetailsController(IConfiguration config)
        {
            _config = config;
        }

        public async Task<IActionResult> Index(int id)
        {
            var model = await GetApplicationDetailsAsync(id);
            if (model == null)
                return NotFound();

            return View("~/Views/Approvers/ApproverApplicationDetails.cshtml", model);
        }

        public async Task<IActionResult> ApprovalFlow(int id)
        {
            var model = await GetApplicationDetailsAsync(id);
            if (model == null)
                return NotFound();

            return View("~/Views/Approvers/ApprovalFlow.cshtml", model);
        }

        public async Task<IActionResult> ApprovalHistory(int id)
        {
            var model = await GetApplicationDetailsAsync(id);
            if (model == null)
                return NotFound();

            return View("~/Views/Approvers/ApprovalHistory.cshtml", model);
        }

        [HttpGet]
        public async Task<JsonResult> GetRoles()
        {
            var roles = new List<object>();

            using (var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand("SELECT RoleID, RoleName FROM Role WHERE IsActive = 1 ORDER BY RoleName", conn))
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            roles.Add(new
                            {
                                roleId = reader.GetInt32(reader.GetOrdinal("RoleID")),
                                roleName = reader.GetString(reader.GetOrdinal("RoleName"))
                            });
                        }
                    }
                }
            }

            return Json(roles);
        }

        [HttpGet]
        public async Task<JsonResult> GetUsersByRole(int roleId)
        {
            var users = new List<object>();

            using (var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(@"
                    SELECT u.UserID, u.FirstName, u.LastName, u.Email 
                    FROM [User] u 
                    WHERE u.RoleID = @RoleID AND u.IsActive = 1
                    ORDER BY u.FirstName, u.LastName", conn))
                {
                    cmd.Parameters.AddWithValue("@RoleID", roleId);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            users.Add(new
                            {
                                userId = reader.GetInt32(reader.GetOrdinal("UserID")),
                                name = $"{reader.GetString(reader.GetOrdinal("FirstName"))} {reader.GetString(reader.GetOrdinal("LastName"))}",
                                email = reader.GetString(reader.GetOrdinal("Email"))
                            });
                        }
                    }
                }
            }

            return Json(users);
        }

        [HttpGet]
        public JsonResult GetCurrentUser()
        {
            var email = HttpContext.Session.GetString("Email");
            var firstName = HttpContext.Session.GetString("FirstName");
            var lastName = HttpContext.Session.GetString("LastName");

            return Json(new
            {
                email = email ?? "",
                name = $"{firstName} {lastName}".Trim()
            });
        }

        [HttpGet]
        public async Task<JsonResult> GetNextPhaseOrder(int loanId)
        {
            var usedOrders = new List<int>();

            using (var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(@"
                    SELECT [Order]
                    FROM LoanApproval
                    WHERE LoanID = @LoanID AND IsActive = 1
                    ORDER BY [Order]", conn))
                {
                    cmd.Parameters.AddWithValue("@LoanID", loanId);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            usedOrders.Add(reader.GetInt32(reader.GetOrdinal("Order")));
                        }
                    }
                }
            }

            int nextOrder = 1;
            if (usedOrders.Count > 0)
            {
                for (int i = 1; i <= 7; i++)
                {
                    if (!usedOrders.Contains(i))
                    {
                        nextOrder = i;
                        break;
                    }
                }
            }

            return Json(new
            {
                nextOrder = nextOrder,
                usedOrders = usedOrders
            });
        }

        [HttpPost]
        public async Task<JsonResult> SaveApprover([FromBody] ApproverSaveApproverRequest request)
        {
            try
            {
                string userName = "";
                string userEmail = "";
                string roleName = "";
                int newLoanApprovalId = 0;

                using (var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();

                    // Check for duplicate before insert
                    using (var dupCmd = new SqlCommand(@"
                        SELECT COUNT(*) FROM LoanApproval
                        WHERE LoanID = @LoanID AND UserID = @UserID AND [Order] = @Order AND IsActive = 1", conn))
                    {
                        dupCmd.Parameters.AddWithValue("@LoanID", request.LoanId);
                        dupCmd.Parameters.AddWithValue("@UserID", request.UserId);
                        dupCmd.Parameters.AddWithValue("@Order", request.PhaseOrder);

                        var alreadyExists = (int)await dupCmd.ExecuteScalarAsync() > 0;
                        if (alreadyExists)
                        {
                            return Json(new { success = false, message = "This approver is already assigned for this order." });
                        }
                    }

                    using (var cmd = new SqlCommand(@"
                        SELECT u.FirstName, u.LastName, u.Email, r.RoleName 
                        FROM [User] u 
                        INNER JOIN Role r ON u.RoleID = r.RoleID 
                        WHERE u.UserID = @UserID AND u.IsActive = 1", conn))
                    {
                        cmd.Parameters.AddWithValue("@UserID", request.UserId);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                userName = $"{reader.GetString(reader.GetOrdinal("FirstName"))} {reader.GetString(reader.GetOrdinal("LastName"))}";
                                userEmail = reader.GetString(reader.GetOrdinal("Email"));
                                roleName = reader.GetString(reader.GetOrdinal("RoleName"));
                            }
                            else
                            {
                                return Json(new { success = false, message = "Selected user not found in the system." });
                            }
                        }
                    }


                    using (var cmd = new SqlCommand(
                        @"INSERT INTO LoanApproval (LoanID, UserID, [Order], Status, Comment, IsActive, CreatedAt, CreatedBy)
      OUTPUT INSERTED.LoanApprovalID
      VALUES (@LoanID, @UserID, @PhaseOrder, 'Pending', @Description, 1, @CreatedAt, @CreatedBy)", conn))
                    {
                        cmd.Parameters.AddWithValue("@LoanID", request.LoanId);
                        cmd.Parameters.AddWithValue("@UserID", request.UserId);
                        cmd.Parameters.AddWithValue("@PhaseOrder", request.PhaseOrder);
                        cmd.Parameters.AddWithValue("@Description", request.Description ?? string.Empty);
                        cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                        cmd.Parameters.AddWithValue("@CreatedBy", HttpContext.Session.GetInt32("UserID")?.ToString() ?? "");

                        newLoanApprovalId = (int)(await cmd.ExecuteScalarAsync() ?? 0);
                    }
                }

                return Json(new
                {
                    success = true,
                    message = "Approver configuration saved successfully!",
                    approverData = new
                    {
                        userId = request.UserId,
                        roleName = roleName,
                        userName = userName,
                        email = userEmail,
                        order = request.PhaseOrder,
                        description = request.Description,
                        loanApprovalId = newLoanApprovalId
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error saving approver: " + ex.Message });
            }
        }

        private async Task<ApproverApplicationDetailsViewModel> GetApplicationDetailsAsync(int id)
        {
            ApproverApplicationDetailsViewModel model = null;
            using (var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand(@"
                    SELECT la.LoanID, la.LoanAmount, la.DateSubmitted, la.ApplicationStatus,
                           u.FirstName, u.LastName, d.DepartmentName
                    FROM LoanApplication la
                    INNER JOIN [User] u ON la.UserID = u.UserID
                    LEFT JOIN Department d ON u.DepartmentID = d.DepartmentID
                    WHERE la.LoanID = @LoanID", conn))
                {
                    cmd.Parameters.AddWithValue("@LoanID", id);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            model = new ApproverApplicationDetailsViewModel
                            {
                                LoanID = reader.GetInt32(reader.GetOrdinal("LoanID")),
                                LoanAmount = reader.GetDecimal(reader.GetOrdinal("LoanAmount")),
                                DateSubmitted = reader.GetDateTime(reader.GetOrdinal("DateSubmitted")),
                                ApplicationStatus = reader["ApplicationStatus"]?.ToString(),
                                EmployeeName = $"{reader["FirstName"]} {reader["LastName"]}",
                                Department = reader["DepartmentName"]?.ToString() ?? "N/A",
                                PayrollAccountNumber = "Credit Proceeds to Account Number",
                                Documents = new List<ApproverDocumentViewModel>(),
                                Approvers = new List<ApproverApproverViewModel>()
                            };
                        }
                    }
                }
                if (model == null)
                    return null;

                // Load all documents for this loan
                using (var cmd = new SqlCommand(@"
    SELECT LoanDocumentID, LoanDocumentName, LoanApprovalID, '' AS [Type]
    FROM LoanDocument
    WHERE LoanID = @LoanID AND IsActive = 1", conn))
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

                            var doc = new ApproverDocumentViewModel
                            {
                                LoanDocumentID = reader.GetInt32(reader.GetOrdinal("LoanDocumentID")),
                                Name = name,
                                Type = type,
                                LoanApprovalID = reader["LoanApprovalID"] != DBNull.Value ? Convert.ToInt32(reader["LoanApprovalID"]) : 0
                            };

                            if (doc.LoanApprovalID == 0)
                                model.LoanerDocuments.Add(doc);
                            else
                                model.ApproverDocuments.Add(doc);
                        }
                    }
                }

                using (var cmd = new SqlCommand(@"
    SELECT la.LoanApprovalID, la.[Order], la.Status, la.Comment, la.ApprovedDate,
           u.UserID, u.FirstName, u.LastName, u.Email, 
           r.RoleID, r.RoleName
    FROM LoanApproval la
    INNER JOIN [User] u ON la.UserID = u.UserID
    INNER JOIN Role r ON u.RoleID = r.RoleID
    WHERE la.LoanID = @LoanID AND la.IsActive = 1
    ORDER BY la.[Order]", conn))
                {
                    cmd.Parameters.AddWithValue("@LoanID", id);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            model.Approvers.Add(new ApproverApproverViewModel
                            {
                                RoleId = reader.GetInt32(reader.GetOrdinal("RoleID")),
                                RoleName = reader.GetString(reader.GetOrdinal("RoleName")),
                                UserId = reader.GetInt32(reader.GetOrdinal("UserID")),
                                UserName = $"{reader.GetString(reader.GetOrdinal("FirstName"))} {reader.GetString(reader.GetOrdinal("LastName"))}",
                                Email = reader.GetString(reader.GetOrdinal("Email")),
                                Order = reader.GetInt32(reader.GetOrdinal("Order")),
                                Description = reader["Comment"]?.ToString() ?? "",
                                Status = reader["Status"]?.ToString() ?? "Pending",
                                ApprovedDate = reader["ApprovedDate"] as DateTime?,
                                LoanApprovalID = reader["LoanApprovalID"] != DBNull.Value ? Convert.ToInt32(reader["LoanApprovalID"]) : 0
                            });
                        }
                    }
                }

                // Add Benefits Assistant review entry if application is beyond Submitted
                // and no Benefits Assistant entry exists yet
                if ((model.ApplicationStatus != "Draft" && model.ApplicationStatus != "Submitted") ||
                    model.ApplicationStatus == "Rejected")
                {
                    if (!model.Approvers.Any(a => a.RoleName.Contains("Benefits Assistant")))
                    {
                        var userId = HttpContext.Session.GetInt32("UserID");
                        var firstName = HttpContext.Session.GetString("FirstName");
                        var lastName = HttpContext.Session.GetString("LastName");
                        var userName = string.IsNullOrEmpty(firstName) ? "Benefits Team" : $"{firstName} {lastName}";

                        string actualComment = string.Empty;
                        try
                        {
                            using (var cmd = new SqlCommand(@"
                                SELECT Comment 
                                FROM LoanApproval 
                                WHERE LoanID = @LoanID AND UserID = @UserID AND IsActive = 1", conn))
                            {
                                cmd.Parameters.AddWithValue("@LoanID", id);
                                cmd.Parameters.AddWithValue("@UserID", userId ?? 0);

                                var result = await cmd.ExecuteScalarAsync();
                                if (result != null && result != DBNull.Value)
                                {
                                    actualComment = result.ToString();
                                }
                            }
                        }
                        catch (Exception) { }

                        model.Approvers.Add(new ApproverApproverViewModel
                        {
                            RoleId = 0,
                            RoleName = "Benefits Assistant",
                            UserId = userId ?? 0,
                            UserName = userName,
                            Email = HttpContext.Session.GetString("Email") ?? "",
                            Order = 0,
                            Description = actualComment,
                            Status = "Reviewed"
                        });
                    }
                }

                var ba = model.Approvers.FirstOrDefault(a => a.RoleName.Contains("Benefits Assistant"));
                var others = model.Approvers
                    .Where(a => !a.RoleName.Contains("Benefits Assistant"))
                    .OrderBy(a =>
                        a.Status == "Pending" ? DateTime.MaxValue :
                        a.Status == "Reviewed" ? DateTime.MaxValue.AddDays(-1) :
                        a.ApprovedDate ?? DateTime.MaxValue
                    )
                    .ToList();

                var newApprovers = new List<ApproverApproverViewModel>();
                if (ba != null) newApprovers.Add(ba);
                newApprovers.AddRange(others);
                model.Approvers = newApprovers;

                // Check if current user has already approved this application
                var currentUserId = HttpContext.Session.GetInt32("UserID");
                if (currentUserId.HasValue)
                {
                    model.CurrentUserHasApproved = model.Approvers.Any(a => 
                        a.UserId == currentUserId.Value && a.Status == "Approved");
                }

                return model;
            }
        }

        public async Task<IActionResult> RejectApplication(int id, string remarks)
        {
            var userId = HttpContext.Session.GetInt32("UserID");

            using (var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                await conn.OpenAsync();

                // Immediately reject the entire LoanApplication when any single approver rejects it
                var cmdUpdateApp = new SqlCommand(@"
                    UPDATE LoanApplication
                    SET Remarks = @Remarks, ApplicationStatus = @Status, ModifiedAt = @ModifiedAt, ModifiedBy = @ModifiedBy
                    WHERE LoanID = @LoanID", conn);
                cmdUpdateApp.Parameters.AddWithValue("@Remarks", remarks ?? string.Empty);
                cmdUpdateApp.Parameters.AddWithValue("@Status", "Rejected");
                cmdUpdateApp.Parameters.AddWithValue("@LoanID", id);
                cmdUpdateApp.Parameters.AddWithValue("@ModifiedAt", DateTime.Now);
                cmdUpdateApp.Parameters.AddWithValue("@ModifiedBy", userId?.ToString() ?? "");

                await cmdUpdateApp.ExecuteNonQueryAsync();

                // Insert or update the LoanApproval record for this approver
                // Check if there's an existing LoanApproval record for this approver and loan
                using (var checkCmd = new SqlCommand(@"
                    SELECT LoanApprovalID 
                    FROM LoanApproval 
                    WHERE LoanID = @LoanID AND UserID = @UserID AND IsActive = 1", conn))
                {
                    checkCmd.Parameters.AddWithValue("@LoanID", id);
                    checkCmd.Parameters.AddWithValue("@UserID", userId);

                    var existingLoanApprovalId = await checkCmd.ExecuteScalarAsync();

                    if (existingLoanApprovalId != null)
                    {
                        // Update existing LoanApproval record to "Rejected"
                        using (var updateApprovalCmd = new SqlCommand(@"
                            UPDATE LoanApproval 
                            SET Status = 'Rejected', 
                                Comment = @Remarks, 
                                ApprovedDate = @ApprovedDate,
                                ModifiedAt = @ModifiedAt,
                                ModifiedBy = @ModifiedBy
                            WHERE LoanApprovalID = @LoanApprovalID", conn))
                        {
                            updateApprovalCmd.Parameters.AddWithValue("@Remarks", remarks ?? string.Empty);
                            updateApprovalCmd.Parameters.AddWithValue("@ApprovedDate", DateTime.Now);
                            updateApprovalCmd.Parameters.AddWithValue("@ModifiedAt", DateTime.Now);
                            updateApprovalCmd.Parameters.AddWithValue("@ModifiedBy", userId?.ToString() ?? "");
                            updateApprovalCmd.Parameters.AddWithValue("@LoanApprovalID", existingLoanApprovalId);

                            await updateApprovalCmd.ExecuteNonQueryAsync();
                        }
                    }
                    else
                    {
                        // Create new LoanApproval record for the rejection
                        var cmdInsertApproval = new SqlCommand(@"
                            INSERT INTO LoanApproval (LoanID, UserID, Status, Comment, [Order], ApprovedDate, IsActive, CreatedAt, CreatedBy)
                            VALUES (@LoanID, @UserID, 'Rejected', @Remarks, 0, @ApprovedDate, 1, @CreatedAt, @CreatedBy)", conn);
                        cmdInsertApproval.Parameters.AddWithValue("@LoanID", id);
                        cmdInsertApproval.Parameters.AddWithValue("@UserID", userId ?? 0);
                        cmdInsertApproval.Parameters.AddWithValue("@Remarks", remarks ?? string.Empty);
                        cmdInsertApproval.Parameters.AddWithValue("@ApprovedDate", DateTime.Now);
                        cmdInsertApproval.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                        cmdInsertApproval.Parameters.AddWithValue("@CreatedBy", userId?.ToString() ?? "");

                        await cmdInsertApproval.ExecuteNonQueryAsync();
                    }
                }
            }

            return RedirectToAction("Index", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadDocuments(int loanId, List<IFormFile> pdfFiles)
        {
            if (pdfFiles == null || pdfFiles.Count == 0)
                return Json(new { success = false, message = "No files uploaded." });

            var savedDocs = new List<ApproverDocumentViewModel>();
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "approvers");
            Directory.CreateDirectory(uploadsFolder);

            foreach (var file in pdfFiles)
            {
                if (Path.GetExtension(file.FileName).ToLower() != ".pdf")
                    return Json(new { success = false, message = "Only PDF files are allowed." });

                var uniqueFileName = $"{loanId}_{Guid.NewGuid()}.pdf";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // TODO: Save document info to your database and link to loanId
                var doc = new ApproverDocumentViewModel
                {
                    Name = file.FileName,
                    Type = "pdf",
                    Url = "/uploads/approvers/" + uniqueFileName
                };
                savedDocs.Add(doc);

                // Example: SaveDocumentToDatabase(loanId, doc);
            }

            return Json(new { success = true, files = savedDocs });
        }

        [HttpGet]
        public async Task<IActionResult> DownloadDocument(int id)
        {
            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            using var cmd = new SqlCommand(@"
        SELECT LoanDocumentName, FileContent
        FROM LoanDocument
        WHERE LoanDocumentID = @LoanDocumentID AND IsActive = 1", conn);
            cmd.Parameters.AddWithValue("@LoanDocumentID", id);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var fileName = reader["LoanDocumentName"]?.ToString() ?? "document.pdf";
                var fileContent = reader["FileContent"] as byte[];
                if (fileContent == null)
                    return NotFound();

                var contentType = "application/pdf";
                Response.Headers.Add("Content-Disposition", $"inline; filename=\"{fileName}\"");
                return File(fileContent, contentType);
            }

            return NotFound();
        }

        [HttpPost]
        public async Task<JsonResult> ApproveApplication([FromBody] ApproverForwardApplicationRequest request)
        {
            try
            {
                using (var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();

                    var approverUserId = HttpContext.Session.GetInt32("UserID");

                    // Check if this is the final approval in the sequence FIRST
                    bool isLastApproval = false;
                    using (var checkLastCmd = new SqlCommand(@"
                        SELECT COUNT(*) 
                        FROM LoanApproval 
                        WHERE LoanID = @LoanID 
                        AND IsActive = 1 
                        AND (Status IS NULL OR Status = 'Pending')
                        AND UserID != @UserID", conn))
                    {
                        checkLastCmd.Parameters.AddWithValue("@LoanID", request.LoanId);
                        checkLastCmd.Parameters.AddWithValue("@UserID", approverUserId);

                        var pendingCount = (int)(await checkLastCmd.ExecuteScalarAsync() ?? 0);
                        isLastApproval = pendingCount == 0;
                    }

                    // Check if there's an existing LoanApproval record for this approver and loan
                    using (var checkCmd = new SqlCommand(@"
                        SELECT LoanApprovalID 
                        FROM LoanApproval 
                        WHERE LoanID = @LoanID AND UserID = @UserID AND IsActive = 1", conn))
                    {
                        checkCmd.Parameters.AddWithValue("@LoanID", request.LoanId);
                        checkCmd.Parameters.AddWithValue("@UserID", approverUserId);

                        var existingLoanApprovalId = await checkCmd.ExecuteScalarAsync();

                        if (existingLoanApprovalId != null)
                        {
                            // Update existing LoanApproval record to "Approved"
                            // No comment for approvals, only for rejections
                            using (var updateApprovalCmd = new SqlCommand(@"
                                UPDATE LoanApproval 
                                SET Status = 'Approved', 
                                    Comment = NULL, 
                                    ApprovedDate = @ApprovedDate,
                                    ModifiedAt = @ModifiedAt,
                                    ModifiedBy = @ModifiedBy
                                WHERE LoanApprovalID = @LoanApprovalID", conn))
                            {
                                updateApprovalCmd.Parameters.AddWithValue("@ApprovedDate", DateTime.Now);
                                updateApprovalCmd.Parameters.AddWithValue("@ModifiedAt", DateTime.Now);
                                updateApprovalCmd.Parameters.AddWithValue("@ModifiedBy", approverUserId?.ToString() ?? "");
                                updateApprovalCmd.Parameters.AddWithValue("@LoanApprovalID", existingLoanApprovalId);

                                await updateApprovalCmd.ExecuteNonQueryAsync();
                            }
                        }
                        else
                        {
                            // Create new LoanApproval record if it doesn't exist
                            // No comment for approvals, only for rejections
                            using (var insertApprovalCmd = new SqlCommand(@"
                                INSERT INTO LoanApproval (LoanID, UserID, [Order], Status, Comment, ApprovedDate, IsActive, CreatedAt, CreatedBy)
                                VALUES (@LoanID, @UserID, 0, 'Approved', NULL, @ApprovedDate, 1, @CreatedAt, @CreatedBy)", conn))
                            {
                                insertApprovalCmd.Parameters.AddWithValue("@LoanID", request.LoanId);
                                insertApprovalCmd.Parameters.AddWithValue("@UserID", approverUserId);
                                insertApprovalCmd.Parameters.AddWithValue("@ApprovedDate", DateTime.Now);
                                insertApprovalCmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                                insertApprovalCmd.Parameters.AddWithValue("@CreatedBy", approverUserId?.ToString() ?? "");

                                await insertApprovalCmd.ExecuteNonQueryAsync();
                            }
                        }
                    }

                    // Update LoanApplication status based on approval state
                    if (isLastApproval)
                    {
                        using (var updateCmd = new SqlCommand(@"
                    UPDATE LoanApplication 
                    SET ApplicationStatus = @ApplicationStatus, 
                        Remarks = @Remarks, 
                        DateAssigned = @DateAssigned,
                        ModifiedAt = @ModifiedAt,
                        ModifiedBy = @ModifiedBy
                    WHERE LoanID = @LoanID", conn))
                        {
                            updateCmd.Parameters.AddWithValue("@ApplicationStatus", "Approved");
                            updateCmd.Parameters.AddWithValue("@Remarks", "Application fully approved by all approvers");
                            updateCmd.Parameters.AddWithValue("@LoanID", request.LoanId);
                            updateCmd.Parameters.AddWithValue("@DateAssigned", DateTime.Now);
                            updateCmd.Parameters.AddWithValue("@ModifiedAt", DateTime.Now);
                            updateCmd.Parameters.AddWithValue("@ModifiedBy", approverUserId?.ToString() ?? "");

                            await updateCmd.ExecuteNonQueryAsync();
                        }

                        return Json(new { success = true, message = "Application fully approved! All approvers have completed their review.", isLastApproval = true });
                    }
                    else
                    {
                        // Update status to 'In Progress' since there are still pending approvers
                        using (var updateCmd = new SqlCommand(@"
                    UPDATE LoanApplication 
                    SET ApplicationStatus = 'In Progress',
                        ModifiedAt = @ModifiedAt,
                        ModifiedBy = @ModifiedBy
                    WHERE LoanID = @LoanID", conn))
                        {
                            updateCmd.Parameters.AddWithValue("@LoanID", request.LoanId);
                            updateCmd.Parameters.AddWithValue("@ModifiedAt", DateTime.Now);
                            updateCmd.Parameters.AddWithValue("@ModifiedBy", approverUserId?.ToString() ?? "");

                            await updateCmd.ExecuteNonQueryAsync();
                        }

                        return Json(new { success = true, message = "Your approval has been recorded successfully! Waiting for other approvers.", isLastApproval = false });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error approving application: " + ex.Message });
            }
        }
        [HttpPost]
        public async Task<IActionResult> UploadApproverDocuments(int? loanId, int? loanApprovalId, List<IFormFile> pdfFiles)
        {
            if (pdfFiles == null || pdfFiles.Count == 0)
            {
                TempData["UploadError"] = "Please select at least one PDF file.";
                return RedirectToAction("ApprovalFlow", new { id = loanId ?? GetLoanIdFromApprovalId(loanApprovalId) });
            }

            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            foreach (var file in pdfFiles)
            {
                if (Path.GetExtension(file.FileName).ToLower() != ".pdf")
                {
                    TempData["UploadError"] = "Only PDF files are allowed.";
                    return RedirectToAction("ApprovalFlow", new { id = loanId ?? GetLoanIdFromApprovalId(loanApprovalId) });
                }

                byte[] fileBytes;
                using (var memoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(memoryStream);
                    fileBytes = memoryStream.ToArray();
                }

                // Only one of loanId or loanApprovalId should be set
                using (var cmd = new SqlCommand(@"
                    INSERT INTO LoanDocument (LoanID, LoanApprovalID, FileContent, LoanDocumentName, IsActive, CreatedAt, CreatedBy)
                    VALUES (@LoanID, @LoanApprovalID, @FileContent, @LoanDocumentName, 1, @CreatedAt, @CreatedBy)", conn))
                {
                    cmd.Parameters.AddWithValue("@LoanID", (object?)loanId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@LoanApprovalID", (object?)loanApprovalId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@FileContent", fileBytes);
                    cmd.Parameters.AddWithValue("@LoanDocumentName", file.FileName);
                    cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                    cmd.Parameters.AddWithValue("@CreatedBy", HttpContext.Session.GetInt32("UserID")?.ToString() ?? "");
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            TempData["UploadSuccess"] = "Files uploaded successfully.";
            return RedirectToAction("ApprovalFlow", new { id = loanId ?? GetLoanIdFromApprovalId(loanApprovalId) });
        }

        private int GetLoanIdFromApprovalId(int? loanApprovalId)
        {
            if (loanApprovalId == null) return 0;
            using var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();
            using var cmd = new SqlCommand("SELECT LoanID FROM LoanApproval WHERE LoanApprovalID = @LoanApprovalID", conn);
            cmd.Parameters.AddWithValue("@LoanApprovalID", loanApprovalId);
            var result = cmd.ExecuteScalar();
            return result != null && result != DBNull.Value ? Convert.ToInt32(result) : 0;
        }

        [HttpGet]
        public async Task<JsonResult> CheckIfFinalApprover(int loanId)
        {
            try
            {
                using (var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();

                    var approverUserId = HttpContext.Session.GetInt32("UserID");

                    // Check if this would be the final approval
                    using (var checkCmd = new SqlCommand(@"
                        SELECT COUNT(*) 
                        FROM LoanApproval 
                        WHERE LoanID = @LoanID 
                        AND IsActive = 1 
                        AND (Status IS NULL OR Status = 'Pending')
                        AND UserID != @UserID", conn))
                    {
                        checkCmd.Parameters.AddWithValue("@LoanID", loanId);
                        checkCmd.Parameters.AddWithValue("@UserID", approverUserId);

                        var pendingCount = (int)(await checkCmd.ExecuteScalarAsync() ?? 0);
                        bool isFinalApprover = pendingCount == 0;

                        return Json(new { success = true, isFinalApprover = isFinalApprover });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error checking approval status: " + ex.Message });
            }
        }
    }
}
