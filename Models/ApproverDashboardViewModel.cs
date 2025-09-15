using System;
using System.Collections.Generic;

namespace StrongHelpOfficial.Models
{
    public class ApproverDashboardViewModel
    {
        public string UserName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public int RoleID { get; set; }

        public int PendingReview { get; set; }
        public int InProgress { get; set; }
        public int Completed { get; set; }
        public List<LoanApplicationViewModel> PendingApplications { get; set; } = new();
        public string SearchQuery { get; set; } = string.Empty;
        public Dictionary<string, bool> Filters { get; set; } = new Dictionary<string, bool>();
        public string DepartmentName { get; set; } = string.Empty;

        // Filter properties
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool ShowFilters { get; set; } = false;
    }

    public class LoanApplicationViewModel
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
