namespace StrongHelpOfficial.Models
{
    public class AdminDashboardViewModel
    {
        public int[] UserID { get; set; }
        public string[] UserName { get; set; }
        public string[] RoleName { get; set; }
        public string[] RoleID { get; set; }
        public string[] Department { get; set; }

        public int UserCount { get; set; }
        public int UserCountLast30Days { get; set; }
        public int ActiveUserCount { get; set; }
        public int InactiveUserCount { get; set; }

    }
}
