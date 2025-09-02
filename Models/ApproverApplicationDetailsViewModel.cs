namespace StrongHelpOfficial.Models
{
    public class ApproverApplicationDetailsViewModel
    {
        public int LoanID { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public decimal LoanAmount { get; set; }
        public string Department { get; set; } = string.Empty;
        public string PayrollAccountNumber { get; set; } = string.Empty;
        public string ApplicationStatus { get; set; } = string.Empty;
        public DateTime DateSubmitted { get; set; }
        public List<ApproverDocumentViewModel> Documents { get; set; } = new();
        public string Remarks { get; set; } = string.Empty;
        public List<ApproverApproverViewModel> Approvers { get; set; } = new();
        public DateTime? ApprovedDate { get; set; } // Nullable DateTime property
        public List<ApproverDocumentViewModel> LoanerDocuments { get; set; } = new List<ApproverDocumentViewModel>();
        public List<ApproverDocumentViewModel> ApproverDocuments { get; set; } = new List<ApproverDocumentViewModel>();
    }

    public class ApproverDocumentViewModel
    {
        public int LoanDocumentID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public int LoanApprovalID { get; set; }
    }

    public class ApproverApproverViewModel
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Order { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? ApprovedDate { get; set; } // Nullable DateTime property
        public int LoanApprovalID { get; set; }
    }

    // Request models for the controller actions
    public class ApproverSaveApproverRequest
    {
        public int LoanId { get; set; }
        public int RoleId { get; set; }
        public int UserId { get; set; }
        public int PhaseOrder { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class ApproverForwardApplicationRequest
    {
        public int LoanId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<ApproverForwardApproverDto> Approvers { get; set; } = new();
    }

    public class ApproverForwardApproverDto
    {
        public int UserId { get; set; }
        public int Order { get; set; }
    }
}
