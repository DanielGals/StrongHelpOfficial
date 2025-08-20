namespace StrongHelpOfficial.Models
{
    public class ApproverApplicationsViewModel
    {
        public string UserName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public int RoleID { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public List<LoanApplicationViewModel> Applications { get; set; } = new();
        public string SelectedTab { get; set; } = string.Empty;
        public string SearchTerm { get; set; } = string.Empty;
        public int TotalApplications { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 5;
    }
}
