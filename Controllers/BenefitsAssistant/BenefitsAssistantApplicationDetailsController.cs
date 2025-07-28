using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using StrongHelpOfficial.Models;
using System;
using System.Collections.Generic;
using System.Data;
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

        // In BenefitsAssistantApplicationDetailsController.cs
        public async Task<IActionResult> ApprovalHistory(int id)
        {
            var model = await GetApplicationDetailsAsync(id);
            if (model == null)
                return NotFound();

            return View("~/Views/BenefitsAssistant/ApprovalHistory.cshtml", model);
        }



        // Get all roles for dropdown
        [HttpGet]
        public async Task<JsonResult> GetRoles()
        {
            var roles = new List<object>();

            using (var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand("SELECT RoleID, RoleName FROM Role ORDER BY RoleName", conn))
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            roles.Add(new
                            {
                                roleId = reader.GetInt32("RoleID"),
                                roleName = reader.GetString("RoleName")
                            });
                        }
                    }
                }
            }

            return Json(roles);
        }

        // Get users by role for dropdown
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
                    WHERE u.RoleID = @RoleID 
                    ORDER BY u.FirstName, u.LastName", conn))
                {
                    cmd.Parameters.AddWithValue("@RoleID", roleId);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            users.Add(new
                            {
                                userId = reader.GetInt32("UserID"),
                                name = $"{reader.GetString("FirstName")} {reader.GetString("LastName")}",
                                email = reader.GetString("Email")
                            });
                        }
                    }
                }
            }

            return Json(users);
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
                    WHERE LoanID = @LoanID
                    ORDER BY [Order]
        ", conn))
                {
                    cmd.Parameters.AddWithValue("@LoanID", loanId);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            usedOrders.Add(reader.GetInt32("Order"));
                        }
                    }
                }
            }

            // Calculate the next available order (if no orders exist, start at 1)
            int nextOrder = 1;
            if (usedOrders.Count > 0)
            {
                // Find the next available order number
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
        // Save approver data
        [HttpPost]
        public async Task<JsonResult> SaveApprover([FromBody] SaveApproverRequest request)
        {
            try
            {
                // Get the user details for the response
                string userName = "";
                string userEmail = "";
                string roleName = "";

                using (var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();

                    // Get user and role details
                    using (var cmd = new SqlCommand(@"
                        SELECT u.FirstName, u.LastName, u.Email, r.RoleName 
                        FROM [User] u 
                        INNER JOIN Role r ON u.RoleID = r.RoleID 
                        WHERE u.UserID = @UserID", conn))
                    {
                        cmd.Parameters.AddWithValue("@UserID", request.UserId);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                userName = $"{reader.GetString("FirstName")} {reader.GetString("LastName")}";
                                userEmail = reader.GetString("Email");
                                roleName = reader.GetString("RoleName");
                            }
                            else
                            {
                                // User not found - return error
                                return Json(new { success = false, message = "Selected user not found in the system." });
                            }
                        }
                    }

                    // Save to LoanApproval table (uncomment if you want to persist to database)
                    /*
                    using (var cmd = new SqlCommand(@"
                        INSERT INTO LoanApproval (LoanID, UserID, [Order], Status, Comments)
                        VALUES (@LoanID, @UserID, @PhaseOrder, 'Pending', @Description)", conn))
                    {
                        cmd.Parameters.AddWithValue("@LoanID", request.LoanId);
                        cmd.Parameters.AddWithValue("@UserID", request.UserId);
                        cmd.Parameters.AddWithValue("@PhaseOrder", request.PhaseOrder);
                        cmd.Parameters.AddWithValue("@Description", request.Description ?? string.Empty);

                        await cmd.ExecuteNonQueryAsync();
                    }
                    */
                }

                // Return data for the UI to create the card - INCLUDE UserId
                return Json(new
                {
                    success = true,
                    message = "Approver configuration saved successfully!",
                    approverData = new
                    {
                        userId = request.UserId, // ADD THIS LINE
                        roleName = roleName,
                        userName = userName,
                        email = userEmail,
                        order = request.PhaseOrder,
                        description = request.Description
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error saving approver: " + ex.Message });
            }
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
                    WHERE la.LoanID = @LoanID", conn))
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
                                Documents = new List<BADocumentViewModel>(),
                                Approvers = new List<ApproverViewModel>()
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

                            model.Documents.Add(new BADocumentViewModel
                            {
                                LoanDocumentID = reader.GetInt32(reader.GetOrdinal("LoanDocumentID")),
                                Name = name,
                                Type = type
                            });
                        }
                    }
                }

                // Fetch existing approvers from LoanApproval table that have been approved or rejected
                using (var cmd = new SqlCommand(@"
                    SELECT la.[Order], la.Status, la.Comment, la.ApprovedDate,
                    u.UserID, u.FirstName, u.LastName, u.Email, 
                    r.RoleID, r.RoleName
                    FROM LoanApproval la
                    INNER JOIN [User] u ON la.ApproverUserID = u.UserID
                    INNER JOIN Role r ON u.RoleID = r.RoleID
                    WHERE la.LoanID = @LoanID
                    AND (la.Status = 'Approved' OR la.Status = 'Rejected' OR la.Status = 'Reviewed')
                    ORDER BY la.[Order]", conn))
                {
                    cmd.Parameters.AddWithValue("@LoanID", id);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            model.Approvers.Add(new ApproverViewModel
                            {
                                RoleId = reader.GetInt32(reader.GetOrdinal("RoleID")),
                                RoleName = reader.GetString(reader.GetOrdinal("RoleName")),
                                UserId = reader.GetInt32(reader.GetOrdinal("UserID")),
                                UserName = $"{reader.GetString(reader.GetOrdinal("FirstName"))} {reader.GetString(reader.GetOrdinal("LastName"))}",
                                Email = reader.GetString(reader.GetOrdinal("Email")),
                                Order = reader.GetInt32(reader.GetOrdinal("Order")),
                                Description = reader["Comment"]?.ToString() ?? "",
                                Status = reader["Status"]?.ToString() ?? "Pending"
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
                        // Get the current user (assuming they are the Benefits Assistant)
                        var userId = HttpContext.Session.GetInt32("UserID");
                        var firstName = HttpContext.Session.GetString("FirstName");
                        var lastName = HttpContext.Session.GetString("LastName");
                        var userName = string.IsNullOrEmpty(firstName) ? "Benefits Team" : $"{firstName} {lastName}";

                        // Get the actual comment from the database instead of using a hardcoded value
                        string actualComment = string.Empty;
                        try
                        {
                            using (var cmd = new SqlCommand(@"
                SELECT Comment 
                FROM LoanApproval 
                WHERE LoanID = @LoanID AND ApproverUserID = @UserID
            ", conn))
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

                        model.Approvers.Add(new ApproverViewModel
                        {
                            RoleId = 0,
                            RoleName = "Benefits Assistant",
                            UserId = userId ?? 0,
                            UserName = userName,
                            Email = HttpContext.Session.GetString("Email") ?? "",
                            Order = 0,  // Always first in sequence
                            Description = actualComment, // Use the actual comment
                            Status = "Reviewed"
                        });
                    }
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

                // Update LoanApplication table
                var cmdUpdateApp = new SqlCommand(@"
                    UPDATE LoanApplication
                    SET Remarks = @Remarks, ApplicationStatus = @Status, IsActive = 0
                    WHERE LoanID = @LoanID", conn);
                cmdUpdateApp.Parameters.AddWithValue("@Remarks", remarks ?? string.Empty);
                cmdUpdateApp.Parameters.AddWithValue("@Status", "Rejected");
                cmdUpdateApp.Parameters.AddWithValue("@LoanID", id);

                await cmdUpdateApp.ExecuteNonQueryAsync();

                // Add a record to the LoanApproval table to indicate rejection by Benefits Assistant
                var cmdInsertApproval = new SqlCommand(@"
            INSERT INTO LoanApproval (LoanID, ApproverUserID, Status, Comment, [Order], ApprovedDate)
            VALUES (@LoanID, @UserID, 'Rejected', @Remarks, 0, @ApprovedDate)
        ", conn);
                cmdInsertApproval.Parameters.AddWithValue("@LoanID", id);
                cmdInsertApproval.Parameters.AddWithValue("@UserID", userId ?? 0);
                cmdInsertApproval.Parameters.AddWithValue("@Remarks", remarks ?? string.Empty);
                cmdInsertApproval.Parameters.AddWithValue("@ApprovedDate", DateTime.Now);

                await cmdInsertApproval.ExecuteNonQueryAsync();
            }

            // Redirect to details or index
            return RedirectToAction("Index", new { id });
        }

        public async Task<JsonResult> ForwardApplication([FromBody] ForwardApplicationRequest request)
        {
            try
            {
                using (var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();

                    // Get the current Benefits Assistant's user ID
                    var benefitsAssistantUserId = HttpContext.Session.GetInt32("UserID");

                    // First, update the LoanApplication table
                    using (var updateCmd = new SqlCommand(@"
                UPDATE LoanApplication 
                SET ApplicationStatus = @ApplicationStatus, 
                    Remarks = @Remarks, 
                    Title = @Title, 
                    Description = @Description,
                    BenefitAssistantUserID = @BenefitAssistantUserID,
                    DateAssigned = @DateAssigned
                WHERE LoanID = @LoanID
            ", conn))
                    {
                        updateCmd.Parameters.AddWithValue("@ApplicationStatus", "In Review");
                        updateCmd.Parameters.AddWithValue("@Remarks", "Waiting for approvers");
                        updateCmd.Parameters.AddWithValue("@Title", request.Title ?? string.Empty);
                        updateCmd.Parameters.AddWithValue("@Description", request.Description ?? string.Empty);
                        updateCmd.Parameters.AddWithValue("@LoanID", request.LoanId);
                        updateCmd.Parameters.AddWithValue("@BenefitAssistantUserID", benefitsAssistantUserId);
                        updateCmd.Parameters.AddWithValue("@DateAssigned", DateTime.Now);

                        await updateCmd.ExecuteNonQueryAsync();
                    }

                    // Add a record for the Benefits Assistant in the LoanApproval table
                    using (var baCmd = new SqlCommand(@"
                INSERT INTO LoanApproval (LoanID, ApproverUserID, [Order], Status, Comment, ApprovedDate)
                VALUES (@LoanID, @ApproverUserID, 0, 'Reviewed', @Comment, @ApprovedDate)
            ", conn))
                    {
                        baCmd.Parameters.AddWithValue("@LoanID", request.LoanId);
                        baCmd.Parameters.AddWithValue("@ApproverUserID", benefitsAssistantUserId);
                        baCmd.Parameters.AddWithValue("@Comment", request.Description ?? "Application reviewed and forwarded");
                        baCmd.Parameters.AddWithValue("@ApprovedDate", DateTime.Now);

                        await baCmd.ExecuteNonQueryAsync();
                    }

                    // Then, insert approvers into LoanApproval table
                    foreach (var approver in request.Approvers)
                    {
                        // First, verify that the UserID exists in the User table
                        using (var checkCmd = new SqlCommand("SELECT COUNT(*) FROM [User] WHERE UserID = @UserID", conn))
                        {
                            checkCmd.Parameters.AddWithValue("@UserID", approver.UserId);
                            var userExists = (int)await checkCmd.ExecuteScalarAsync() > 0;

                            if (!userExists)
                            {
                                return Json(new { success = false, message = $"User with ID {approver.UserId} not found in the system." });
                            }
                        }

                        // Insert into LoanApproval table - only specify the columns we have values for
                        using (var cmd = new SqlCommand(@"
                    INSERT INTO LoanApproval (LoanID, ApproverUserID, [Order])
                    VALUES (@LoanID, @ApproverUserID, @Order)
                ", conn))
                        {
                            cmd.Parameters.AddWithValue("@LoanID", request.LoanId);
                            cmd.Parameters.AddWithValue("@ApproverUserID", approver.UserId);
                            cmd.Parameters.AddWithValue("@Order", approver.Order);

                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error forwarding application: " + ex.Message });
            }
        }
    }
}