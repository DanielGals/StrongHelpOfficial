namespace StrongHelpOfficial.Models
{
    public class LoanApplicationDetailsViewModel
    {
        public int LoanID { get; set; }
        public decimal LoanAmount { get; set; }
        public DateTime DateSubmitted { get; set; }
        public string ApplicationStatus { get; set; }
        public string EmployeeName { get; set; }
        public string Department { get; set; }
        public string PayrollAccountNumber { get; set; }
        public List<DocumentViewModel> Documents { get; set; }
        public List<ApprovalViewModel> ApprovalHistory { get; set; } = new List<ApprovalViewModel>();
        public int? BenefitAssistantUserID { get; set; }
        public string BenefitAssistantName { get; set; }
        public DateTime? DateAssigned { get; set; }
        public string Remarks { get; set; }
        public List<ApprovalViewModel> Approvers => ApprovalHistory;
        public List<BADocumentViewModel> LoanerDocuments { get; set; } = new List<BADocumentViewModel>();
        public string? CoMakerName { get; set; }
    }

    public class DocumentViewModel
    {
        public int LoanDocumentID { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Url { get; set; }
        public int? LoanApprovalID { get; set; }
    }
}