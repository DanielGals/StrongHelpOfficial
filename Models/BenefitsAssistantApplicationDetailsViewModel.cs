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


    }

    public class BADocumentViewModel
    {
        public int LoanDocumentID { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
    }

}
