namespace StrongHelpOfficial.Models
{
    public class MyApplicationViewModel
    {
        public int LoanID { get; set; }
        public decimal LoanAmount { get; set; }
        public DateTime DateSubmitted { get; set; }
        public bool IsActive { get; set; }
        public int? BenefitAssistantUserID { get; set; }
        public DateTime? DateAssigned { get; set; }
        public string? ApplicationStatus { get; set; }
        public string? Remarks { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }

        // Optional: For display purposes
        public int ProgressPercent { get; set; }
    }
}
