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

        public async Task<IActionResult> ApprovalHistory(int id)
        {
            var model = await GetApplicationDetailsAsync(id);
            if (model == null)
                return NotFound();

            // Use the BenefitsAssistant-specific ApprovalHistory view
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

                // Fetch existing approvers from LoanApproval table (if you want to load existing data)
                /*
                using (var cmd = new SqlCommand(@"
                    SELECT la.[Order], u.UserID, u.FirstName, u.LastName, u.Email, r.RoleID, r.RoleName, la.Comments
                    FROM LoanApproval la
                    INNER JOIN [User] u ON la.UserID = u.UserID
                    INNER JOIN Role r ON u.RoleID = r.RoleID
                    WHERE la.LoanID = @LoanID
                    ORDER BY la.[Order]
                ", conn))
                {
                    cmd.Parameters.AddWithValue("@LoanID", id);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            model.Approvers.Add(new ApproverViewModel
                            {
                                RoleId = reader.GetInt32("RoleID"),
                                RoleName = reader.GetString("RoleName"),
                                UserId = reader.GetInt32("UserID"),
                                UserName = $"{reader.GetString("FirstName")} {reader.GetString("LastName")}",
                                Email = reader.GetString("Email"),
                                Order = reader.GetInt32("Order"),
                                Description = reader["Comments"]?.ToString() ?? ""
                            });
                        }
                    }
                }
                */
            }
            return model;
        }
        public async Task<IActionResult> RejectApplication(int id, string remarks)
        {
            using (var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand(@"
            UPDATE LoanApplication
            SET Remarks = @Remarks, ApplicationStatus = @Status
            WHERE LoanID = @LoanID
        ", conn);
                cmd.Parameters.AddWithValue("@Remarks", remarks ?? string.Empty);
                cmd.Parameters.AddWithValue("@Status", "Rejected");
                cmd.Parameters.AddWithValue("@LoanID", id);

                await cmd.ExecuteNonQueryAsync();
            }

            // Optionally, redirect to details or index
            return RedirectToAction("Index", new { id });
        }
        public async Task<JsonResult> ForwardApplication([FromBody] ForwardApplicationRequest request)
        {
            try
            {
                using (var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();

                    // First, update the LoanApplication table
                    using (var updateCmd = new SqlCommand(@"
                UPDATE LoanApplication 
                SET ApplicationStatus = @ApplicationStatus, 
                    Remarks = @Remarks, 
                    Title = @Title, 
                    Description = @Description
                WHERE LoanID = @LoanID
            ", conn))
                    {
                        updateCmd.Parameters.AddWithValue("@ApplicationStatus", "In Review");
                        updateCmd.Parameters.AddWithValue("@Remarks", "Waiting for approvers");
                        updateCmd.Parameters.AddWithValue("@Title", request.Title ?? string.Empty);
                        updateCmd.Parameters.AddWithValue("@Description", request.Description ?? string.Empty);
                        updateCmd.Parameters.AddWithValue("@LoanID", request.LoanId);

                        await updateCmd.ExecuteNonQueryAsync();
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