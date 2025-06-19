using System.Collections.Generic;

namespace StrongHelpOfficial.Models
{
    public class LoanerLoanHistoryViewModel
    {
        public List<MyApplicationViewModel> Applications { get; set; } = new();
        public List<string> Statuses { get; set; } = new();
        public string? SelectedStatus { get; set; }
    }
}
