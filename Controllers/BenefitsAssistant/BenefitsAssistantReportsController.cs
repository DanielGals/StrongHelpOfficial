using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using StrongHelpOfficial.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace StrongHelpOfficial.Controllers.BenefitsAssistant
{
    public class BenefitsAssistantReportsController : Controller
    {
        private readonly IConfiguration _configuration;

        public BenefitsAssistantReportsController(IConfiguration configuration)
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

            var model = new BenefitsAssistantReportsViewModel
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
                return View("~/Views/BenefitsAssistant/BenefitsAssistantReports.cshtml", model);
            }

            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            int benefitsAssistantUserId = 0;
            using (var conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();

                using (var cmd = new SqlCommand("SELECT UserID FROM [User] WHERE Email = @Email AND IsActive = 1", conn))
                {
                    cmd.Parameters.AddWithValue("@Email", email);
                    var result = await cmd.ExecuteScalarAsync();
                    if (result != null)
                    {
                        benefitsAssistantUserId = (int)result;
                    }
                }

                await LoadOverviewStatistics(conn, model, benefitsAssistantUserId, isFilter);
                await LoadApplicationsByStatus(conn, model, benefitsAssistantUserId, isFilter);

                model.ApprovedCount = model.ApplicationsByStatus
                    .FirstOrDefault(x => x.Status == "Approved")?.Count ?? 0;
                model.RejectedCount = model.ApplicationsByStatus
                    .FirstOrDefault(x => x.Status == "Rejected")?.Count ?? 0;
                model.SubmittedCount = model.ApplicationsByStatus
                    .FirstOrDefault(x => x.Status == "Submitted")?.Count ?? 0;
                model.InReviewCount = model.ApplicationsByStatus
                    .FirstOrDefault(x => x.Status == "In Review")?.Count ?? 0;

                await LoadMonthlyTrends(conn, model, benefitsAssistantUserId, isFilter);
                await LoadTopLoanTypes(conn, model, benefitsAssistantUserId, isFilter);
            }

            if (isFilter && model.TotalApplications == 0)
            {
                TempData["NoDataMessage"] = "No loan applications found for the selected date range.";
            }

            return View("~/Views/BenefitsAssistant/BenefitsAssistantReports.cshtml", model);
        }

        private async Task LoadOverviewStatistics(SqlConnection conn, BenefitsAssistantReportsViewModel model, int userId, bool isFilter)
        {
            string query;
            if (isFilter)
            {
                query = @"
                    SELECT 
                        COUNT(*) AS TotalApplications,
                        SUM(CASE WHEN ISNULL(ApplicationStatus, 'Submitted') = 'Submitted' THEN 1 ELSE 0 END) AS SubmittedCount,
                        SUM(CASE WHEN ISNULL(ApplicationStatus, 'Submitted') = 'In Review' THEN 1 ELSE 0 END) AS InReviewCount,
                        SUM(CASE WHEN ISNULL(ApplicationStatus, 'Submitted') = 'Approved' THEN 1 ELSE 0 END) AS ApprovedCount,
                        SUM(CASE WHEN ISNULL(ApplicationStatus, 'Submitted') = 'Rejected' THEN 1 ELSE 0 END) AS RejectedCount,
                        SUM(CASE WHEN ISNULL(ApplicationStatus, 'Submitted') = 'Approved' THEN LoanAmount ELSE 0 END) AS TotalApprovedAmount,
                        AVG(CASE WHEN ISNULL(ApplicationStatus, 'Submitted') = 'Approved' THEN LoanAmount ELSE NULL END) AS AverageApprovedAmount
                    FROM LoanApplication
                    WHERE (BenefitsAssistantUserID = @UserId OR BenefitsAssistantUserID IS NULL)
                    AND UserID != @UserId
                    AND (IsActive = 1 OR ApplicationStatus = 'Rejected')
                    AND CAST(DateSubmitted AS DATE) >= @StartDate
                    AND CAST(DateSubmitted AS DATE) <= @EndDate
                ";
            }
            else
            {
                query = @"
                    SELECT 
                        COUNT(*) AS TotalApplications,
                        SUM(CASE WHEN ISNULL(ApplicationStatus, 'Submitted') = 'Submitted' THEN 1 ELSE 0 END) AS SubmittedCount,
                        SUM(CASE WHEN ISNULL(ApplicationStatus, 'Submitted') = 'In Review' THEN 1 ELSE 0 END) AS InReviewCount,
                        SUM(CASE WHEN ISNULL(ApplicationStatus, 'Submitted') = 'Approved' THEN 1 ELSE 0 END) AS ApprovedCount,
                        SUM(CASE WHEN ISNULL(ApplicationStatus, 'Submitted') = 'Rejected' THEN 1 ELSE 0 END) AS RejectedCount,
                        SUM(CASE WHEN ISNULL(ApplicationStatus, 'Submitted') = 'Approved' THEN LoanAmount ELSE 0 END) AS TotalApprovedAmount,
                        AVG(CASE WHEN ISNULL(ApplicationStatus, 'Submitted') = 'Approved' THEN LoanAmount ELSE NULL END) AS AverageApprovedAmount
                    FROM LoanApplication
                    WHERE (BenefitsAssistantUserID = @UserId OR BenefitsAssistantUserID IS NULL)
                    AND UserID != @UserId
                    AND (IsActive = 1 OR ApplicationStatus = 'Rejected')
                ";
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
                        model.SubmittedCount = reader.IsDBNull(reader.GetOrdinal("SubmittedCount")) ? 0 : reader.GetInt32(reader.GetOrdinal("SubmittedCount"));
                        model.InReviewCount = reader.IsDBNull(reader.GetOrdinal("InReviewCount")) ? 0 : reader.GetInt32(reader.GetOrdinal("InReviewCount"));
                        model.ApprovedCount = reader.IsDBNull(reader.GetOrdinal("ApprovedCount")) ? 0 : reader.GetInt32(reader.GetOrdinal("ApprovedCount"));
                        model.RejectedCount = reader.IsDBNull(reader.GetOrdinal("RejectedCount")) ? 0 : reader.GetInt32(reader.GetOrdinal("RejectedCount"));
                        model.TotalApprovedAmount = reader.IsDBNull(reader.GetOrdinal("TotalApprovedAmount")) ? 0 : reader.GetDecimal(reader.GetOrdinal("TotalApprovedAmount"));
                        model.AverageApprovedAmount = reader.IsDBNull(reader.GetOrdinal("AverageApprovedAmount")) ? 0 : reader.GetDecimal(reader.GetOrdinal("AverageApprovedAmount"));
                        model.ApplicationsInPeriod = model.TotalApplications;
                    }
                }
            }
        }

        private async Task LoadApplicationsByStatus(SqlConnection conn, BenefitsAssistantReportsViewModel model, int userId, bool isFilter)
        {
            string query = @"
                SELECT ISNULL(ApplicationStatus, 'Submitted') as ApplicationStatus, COUNT(*) as Count
                FROM LoanApplication
                WHERE (BenefitsAssistantUserID = @UserId OR BenefitsAssistantUserID IS NULL)
                AND UserID != @UserId
                AND (IsActive = 1 OR ApplicationStatus = 'Rejected')
            ";
            if (isFilter)
            {
                query += @"
                    AND CAST(DateSubmitted AS DATE) >= @StartDate 
                    AND CAST(DateSubmitted AS DATE) <= @EndDate
                ";
            }
            query += @"
                GROUP BY ApplicationStatus
                ORDER BY Count DESC
            ";

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
                            Status = reader.GetString("ApplicationStatus"),
                            Count = reader.GetInt32("Count")
                        });
                    }
                }
            }
        }

        private async Task LoadMonthlyTrends(SqlConnection conn, BenefitsAssistantReportsViewModel model, int userId, bool isFilter)
        {
            string query = @"
                SELECT 
                    YEAR(DateSubmitted) as Year,
                    MONTH(DateSubmitted) as Month,
                    COUNT(*) as ApplicationCount,
                    SUM(CASE WHEN ISNULL(ApplicationStatus, 'Submitted') = 'Approved' THEN 1 ELSE 0 END) as ApprovedCount,
                    SUM(CASE WHEN ISNULL(ApplicationStatus, 'Submitted') = 'Approved' THEN LoanAmount ELSE 0 END) as ApprovedAmount
                FROM LoanApplication
                WHERE (BenefitsAssistantUserID = @UserId OR BenefitsAssistantUserID IS NULL)
                AND UserID != @UserId
                AND (IsActive = 1 OR ApplicationStatus = 'Rejected')
            ";
            if (isFilter)
            {
                query += @"
                    AND CAST(DateSubmitted AS DATE) >= @StartDate
                    AND CAST(DateSubmitted AS DATE) <= @EndDate
                ";
            }
            else
            {
                query += @"
                    AND DateSubmitted >= DATEADD(MONTH, -6, GETDATE())
                ";
            }
            query += @"
                GROUP BY YEAR(DateSubmitted), MONTH(DateSubmitted)
                ORDER BY YEAR(DateSubmitted), MONTH(DateSubmitted)
            ";

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

        private async Task LoadTopLoanTypes(SqlConnection conn, BenefitsAssistantReportsViewModel model, int userId, bool isFilter)
        {
            string query = @"
                SELECT TOP 5
                    ISNULL(Title, 'General Loan') as LoanType,
                    COUNT(*) as Count,
                    SUM(LoanAmount) as TotalAmount
                FROM LoanApplication
                WHERE (BenefitsAssistantUserID = @UserId OR BenefitsAssistantUserID IS NULL)
                AND UserID != @UserId
                AND (IsActive = 1 OR ApplicationStatus = 'Rejected')
            ";
            if (isFilter)
            {
                query += @"
                    AND CAST(DateSubmitted AS DATE) >= @StartDate 
                    AND CAST(DateSubmitted AS DATE) <= @EndDate
                ";
            }
            query += @"
                GROUP BY Title
                ORDER BY Count DESC
            ";

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