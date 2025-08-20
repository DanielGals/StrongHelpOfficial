namespace StrongHelpOfficial.Models
{
    public class BenefitsAssistantApplicationDetailsViewModel
    {
        public int LoanID { get; set; }
        public string EmployeeName { get; set; }
        public decimal LoanAmount { get; set; }
        public string Department { get; set; }
        public string PayrollAccountNumber { get; set; }
        public string ApplicationStatus { get; set; }
        public DateTime DateSubmitted { get; set; }
        public List<BADocumentViewModel> Documents { get; set; } = new();
        public string Remarks { get; set; } = string.Empty;
        public List<ApproverViewModel> Approvers { get; set; } = new();
        public DateTime? ApprovedDate { get; set; } // Nullable DateTime property
        public List<BADocumentViewModel> LoanerDocuments { get; set; } = new List<BADocumentViewModel>();
        public List<BADocumentViewModel> ApproverDocuments { get; set; } = new List<BADocumentViewModel>();
    }

    public class BADocumentViewModel
    {
        public int LoanDocumentID { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }

        public string Url { get; set; }

        public int LoanApprovalID { get; set; }

    }

    public class ApproverViewModel
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public int Order { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public DateTime? ApprovedDate { get; set; } // Nullable DateTime property
        public int LoanApprovalID { get; set; }
    }

    // Request models for the controller actions
    public class SaveApproverRequest
    {
        public int LoanId { get; set; }
        public int RoleId { get; set; }
        public int UserId { get; set; }
        public int PhaseOrder { get; set; }
        public string Description { get; set; }
    }

    public class ForwardApplicationRequest
    {
        public int LoanId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public List<ForwardApproverDto> Approvers { get; set; }
    }

    public class ForwardApproverDto
    {
        public int UserId { get; set; }
        public int Order { get; set; }
    }
}