using System;
using System.Collections.Generic;

namespace StrongHelpOfficial.Models
{
    public class BenefitsAssistantApplicationsViewModel
    {
        public string? SelectedTab { get; set; }
        public string? SearchTerm { get; set; }
        public List<BenefitsApplicationViewModel> Applications { get; set; } = new();

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 5;
        public int TotalApplications { get; set; }
    }

    public class BenefitsApplicationViewModel
    {
        public string ApplicationId { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string Initials { get; set; } = string.Empty;
        public string LoanType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime DateApplied { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
