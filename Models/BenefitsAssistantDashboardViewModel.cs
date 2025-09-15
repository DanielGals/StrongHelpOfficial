using System;
using System.Collections.Generic;

namespace StrongHelpOfficial.Models
{
    public class BenefitsAssistantDashboardViewModel
    {
        public string UserName { get; set; } = string.Empty;
        public int TotalApplications { get; set; }
        public int PendingReview { get; set; }
        public int InProgress { get; set; }
        public int TotalApproved { get; set; }
        public int TotalRejected { get; set; }
        public List<PendingApplicationViewModel> PendingApplications { get; set; } = new();
    }

    public class PendingApplicationViewModel
{
    public int ApplicationId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
    public string LoanType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime DateApplied { get; set; }
    public string Status { get; set; } = string.Empty;
}

}
