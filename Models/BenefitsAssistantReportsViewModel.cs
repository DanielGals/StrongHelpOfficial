using System;
using System.Collections.Generic;

namespace StrongHelpOfficial.Models
{
    public class BenefitsAssistantReportsViewModel
    {
        public DateTime StartDate { get; set; } = DateTime.Today.AddDays(-30);
        public DateTime EndDate { get; set; } = DateTime.Today;
        public string ReportType { get; set; } = "overview";

        // Overview Statistics
        public int TotalApplications { get; set; }
        public int SubmittedCount { get; set; }
        public int InReviewCount { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }
        public decimal TotalApprovedAmount { get; set; }
        public decimal AverageApprovedAmount { get; set; }
        public int ApplicationsInPeriod { get; set; }

        // Calculated Properties
        public decimal ApprovalRate => TotalApplications > 0 ? (decimal)ApprovedCount / TotalApplications * 100 : 0;
        public decimal RejectionRate => TotalApplications > 0 ? (decimal)RejectedCount / TotalApplications * 100 : 0;

        // Chart Data
        public List<StatusCountViewModel> ApplicationsByStatus { get; set; } = new();
        public List<MonthlyTrendViewModel> MonthlyTrends { get; set; } = new();
        public List<LoanTypeStatsViewModel> TopLoanTypes { get; set; } = new();
    }

    public class StatusCountViewModel
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class MonthlyTrendViewModel
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int ApplicationCount { get; set; }
        public int ApprovedCount { get; set; }
        public decimal ApprovedAmount { get; set; }

        public string MonthName => new DateTime(Year, Month, 1).ToString("MMM yyyy");
    }

    public class LoanTypeStatsViewModel
    {
        public string LoanType { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
