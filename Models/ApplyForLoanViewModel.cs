using System.ComponentModel.DataAnnotations;

namespace StrongHelpOfficial.Models
{
    public class ApplyForLoanViewModel
    {
        [Required(ErrorMessage = "Uploaded documents are required!")]
        public List<IFormFile> Files { get; set; }
        //LoanDocument
        public int LoanDocumentID { get; set; }
        public byte[] Filecontent { get; set; } = Array.Empty<byte>();
        public string[]? LoanDocumentName { get; set; }
        //LoanApplication
        public int LoanID { get; set; }
        public int UserID { get; set; }
        [Required(ErrorMessage = "Loan amount is required.")]
        public int LoanAmount { get; set; }
        public DateTime DateSubmitted { get; set; }
        public bool? isActive { get; set; }
        public int BenefitsAssistantUserID { get; set; }
        public DateTime DateAssigned { get; set; }
        public string? ApplicationStatus { get; set; } = string.Empty;
        public string? Remarks { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string? Description { get; set; }

        // Add property for required documents
        public List<string> RequiredDocuments { get; set; } = new List<string>();
    }
}
