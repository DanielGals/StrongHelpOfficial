namespace StrongHelpOfficial.Models
{
    public class LoanerDashboardViewModel
    {
        public string UserName { get; set; } = string.Empty;
        public int ActiveLoans { get; set; }
        public int PendingApplications { get; set; }
        public IEnumerable<string> RecentActivities { get; set; } = Enumerable.Empty<string>();
    }
}
