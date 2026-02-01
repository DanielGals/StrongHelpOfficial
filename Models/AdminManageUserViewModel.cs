namespace StrongHelpOfficial.Models
{
    public class AdminManageUserViewModel
    {
        public string firstName { get; set; } = string.Empty;
        public string lastName { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string department { get; set; } = string.Empty; // Department ID
        public string departmentName { get; set; } = string.Empty; // Department Name
        public string userId { get; set; } = string.Empty;
        public string role { get; set; } = string.Empty; // Role ID
        public string roleName { get; set; } = string.Empty; // Role Name
        public int isActive { get; set; } = 0;
        public string createdAt { get; set; } = string.Empty;
        public string modifiedAt { get; set; } = string.Empty;
    }
}
