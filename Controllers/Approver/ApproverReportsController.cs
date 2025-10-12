using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using StrongHelpOfficial.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace StrongHelpOfficial.Controllers.Approver
{
    public class ApproverReportsController : Controller
    {
        private readonly IConfiguration _configuration;

        public ApproverReportsController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<IActionResult> Index(DateTime? startDate = null, DateTime? endDate = null, string reportType = "overview")
        {
            var email = HttpContext.Session.GetString("Email");
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Login", "Auth");
            }

            bool isFilter = startDate.HasValue && endDate.HasValue;
            DateTime defaultEnd = DateTime.Today;
            DateTime defaultStart = defaultEnd.AddDays(-30);

            var model = new ApproverReportsViewModel
            {
                StartDate = isFilter ? startDate.Value : defaultStart,
                EndDate = isFilter ? endDate.Value : defaultEnd,
                ReportType = reportType,
                ApplicationsByStatus = new List<StatusCountViewModel>(),
                MonthlyTrends = new List<MonthlyTrendViewModel>(),
                TopLoanTypes = new List<LoanTypeStatsViewModel>()
            };

            if (isFilter && startDate > endDate)
            {
                TempData["Message"] = "Start date cannot be after end date.";
                return View("~/Views/Approvers/ApproverReports.cshtml", model);
            }

            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            int approverUserId = 0;
            using (var conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();

                using (var cmd = new SqlCommand("SELECT UserID FROM [User] WHERE Email = @Email AND IsActive = 1", conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    var result = await cmd.ExecuteScalarAsync();
                    if (result != null)
                    {
                        approverUserId = (int)result;
                    }
                }

                await LoadOverviewStatistics(conn, model, approverUserId, isFilter);
                await LoadApplicationsByStatus(conn, model, approverUserId, isFilter);

                model.ApprovedCount = model.ApplicationsByStatus
                    .FirstOrDefault(x => x.Status == "Approved")?.Count ?? 0;
                model.RejectedCount = model.ApplicationsByStatus
                    .FirstOrDefault(x => x.Status == "Rejected")?.Count ?? 0;
                model.SubmittedCount = model.ApplicationsByStatus
                    .FirstOrDefault(x => x.Status == "Pending")?.Count ?? 0;
                model.InReviewCount = model.ApplicationsByStatus
                    .FirstOrDefault(x => x.Status == "Pending")?.Count ?? 0;

                await LoadMonthlyTrends(conn, model, approverUserId, isFilter);
                await LoadTopLoanTypes(conn, model, approverUserId, isFilter);
            }

            if (isFilter && model.TotalApplications == 0)
            {
                TempData["NoDataMessage"] = "No loan applications found for the selected date range.";
            }

            return View("~/Views/Approvers/ApproverReports.cshtml", model);
        }

        private async Task LoadOverviewStatistics(SqlConnection conn, ApproverReportsViewModel model, int userId, bool isFilter)
        {
            string query = @"
                SELECT 
                    COUNT(DISTINCT la.LoanID) AS TotalApplications,
                    COUNT(DISTINCT CASE WHEN lap.Status = 'Approved' THEN la.LoanID END) AS ApprovedCount,
                    COUNT(DISTINCT CASE WHEN lap.Status = 'Rejected' THEN la.LoanID END) AS RejectedCount,
                    SUM(CASE WHEN lap.Status = 'Approved' THEN la.LoanAmount ELSE 0 END) AS TotalApprovedAmount,
                    AVG(CASE WHEN lap.Status = 'Approved' THEN la.LoanAmount ELSE NULL END) AS AverageApprovedAmount
                FROM LoanApproval lap
                INNER JOIN LoanApplication la ON lap.LoanID = la.LoanID
                WHERE lap.UserID = @UserId
                AND lap.IsActive = 1
                AND (la.IsActive = 1 OR la.ApplicationStatus = 'Rejected')
                AND la.UserID != @UserId";

            if (isFilter)
            {
                query += " AND CAST(lap.ApprovedDate AS DATE) >= @StartDate AND CAST(lap.ApprovedDate AS DATE) <= @EndDate";
            }

            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                if (isFilter)
                {
                    cmd.Parameters.AddWithValue("@StartDate", model.StartDate.Date);
                    cmd.Parameters.AddWithValue("@EndDate", model.EndDate.Date);
                }

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        model.TotalApplications = reader.IsDBNull(reader.GetOrdinal("TotalApplications")) ? 0 : reader.GetInt32(reader.GetOrdinal("TotalApplications"));
                        model.ApprovedCount = reader.IsDBNull(reader.GetOrdinal("ApprovedCount")) ? 0 : reader.GetInt32(reader.GetOrdinal("ApprovedCount"));
                        model.RejectedCount = reader.IsDBNull(reader.GetOrdinal("RejectedCount")) ? 0 : reader.GetInt32(reader.GetOrdinal("RejectedCount"));
                        model.TotalApprovedAmount = reader.IsDBNull(reader.GetOrdinal("TotalApprovedAmount")) ? 0 : reader.GetDecimal(reader.GetOrdinal("TotalApprovedAmount"));
                        model.AverageApprovedAmount = reader.IsDBNull(reader.GetOrdinal("AverageApprovedAmount")) ? 0 : reader.GetDecimal(reader.GetOrdinal("AverageApprovedAmount"));
                        model.ApplicationsInPeriod = model.TotalApplications;
                    }
                }
            }
        }

        private async Task LoadApplicationsByStatus(SqlConnection conn, ApproverReportsViewModel model, int userId, bool isFilter)
        {
            string query = @"
                SELECT 
                    ISNULL(lap.Status, 'Pending') as Status, 
                    COUNT(DISTINCT la.LoanID) as Count
                FROM LoanApproval lap
                INNER JOIN LoanApplication la ON lap.LoanID = la.LoanID
                WHERE lap.UserID = @UserId
                AND lap.IsActive = 1
                AND (la.IsActive = 1 OR la.ApplicationStatus = 'Rejected')
                AND la.UserID != @UserId";

            if (isFilter)
            {
                query += " AND CAST(lap.ApprovedDate AS DATE) >= @StartDate AND CAST(lap.ApprovedDate AS DATE) <= @EndDate";
            }

            query += " GROUP BY lap.Status ORDER BY Count DESC";

            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                if (isFilter)
                {
                    cmd.Parameters.AddWithValue("@StartDate", model.StartDate.Date);
                    cmd.Parameters.AddWithValue("@EndDate", model.EndDate.Date);
                }

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        model.ApplicationsByStatus.Add(new StatusCountViewModel
                        {
                            Status = reader.GetString("Status"),
                            Count = reader.GetInt32("Count")
                        });
                    }
                }
            }
        }

        private async Task LoadMonthlyTrends(SqlConnection conn, ApproverReportsViewModel model, int userId, bool isFilter)
        {
            string query = @"
                SELECT 
                    YEAR(lap.ApprovedDate) as Year,
                    MONTH(lap.ApprovedDate) as Month,
                    COUNT(*) as ApplicationCount,
                    SUM(CASE WHEN lap.Status = 'Approved' THEN 1 ELSE 0 END) as ApprovedCount,
                    SUM(CASE WHEN lap.Status = 'Approved' THEN la.LoanAmount ELSE 0 END) as ApprovedAmount
                FROM LoanApproval lap
                INNER JOIN LoanApplication la ON lap.LoanID = la.LoanID
                WHERE lap.UserID = @UserId
                AND lap.IsActive = 1
                AND lap.ApprovedDate IS NOT NULL
                AND la.UserID != @UserId";

            if (isFilter)
            {
                query += " AND CAST(lap.ApprovedDate AS DATE) >= @StartDate AND CAST(lap.ApprovedDate AS DATE) <= @EndDate";
            }
            else
            {
                query += " AND lap.ApprovedDate >= DATEADD(MONTH, -6, GETDATE())";
            }

            query += " GROUP BY YEAR(lap.ApprovedDate), MONTH(lap.ApprovedDate) ORDER BY YEAR(lap.ApprovedDate), MONTH(lap.ApprovedDate)";

            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                if (isFilter)
                {
                    cmd.Parameters.AddWithValue("@StartDate", model.StartDate.Date);
                    cmd.Parameters.AddWithValue("@EndDate", model.EndDate.Date);
                }

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        model.MonthlyTrends.Add(new MonthlyTrendViewModel
                        {
                            Year = reader.GetInt32("Year"),
                            Month = reader.GetInt32("Month"),
                            ApplicationCount = reader.GetInt32("ApplicationCount"),
                            ApprovedCount = reader.GetInt32("ApprovedCount"),
                            ApprovedAmount = reader.GetDecimal("ApprovedAmount")
                        });
                    }
                }
            }
        }

        private async Task LoadTopLoanTypes(SqlConnection conn, ApproverReportsViewModel model, int userId, bool isFilter)
        {
            string query = @"
                SELECT TOP 5
                    ISNULL(la.Title, 'General Loan') as LoanType,
                    COUNT(*) as Count,
                    SUM(la.LoanAmount) as TotalAmount
                FROM LoanApproval lap
                INNER JOIN LoanApplication la ON lap.LoanID = la.LoanID
                WHERE lap.UserID = @UserId
                AND lap.IsActive = 1
                AND (la.IsActive = 1 OR la.ApplicationStatus = 'Rejected')
                AND la.UserID != @UserId";

            if (isFilter)
            {
                query += " AND CAST(lap.ApprovedDate AS DATE) >= @StartDate AND CAST(lap.ApprovedDate AS DATE) <= @EndDate";
            }

            query += " GROUP BY la.Title ORDER BY Count DESC";

            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                if (isFilter)
                {
                    cmd.Parameters.AddWithValue("@StartDate", model.StartDate.Date);
                    cmd.Parameters.AddWithValue("@EndDate", model.EndDate.Date);
                }

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        model.TopLoanTypes.Add(new LoanTypeStatsViewModel
                        {
                            LoanType = reader.GetString("LoanType"),
                            Count = reader.GetInt32("Count"),
                            TotalAmount = reader.GetDecimal("TotalAmount")
                        });
                    }
                }
            }
        }
    }
}