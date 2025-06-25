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
    }

    public class DocumentViewModel
    {
        public int LoanDocumentID { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
    }
}


