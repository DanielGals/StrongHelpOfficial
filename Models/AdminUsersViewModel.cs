using System.Collections.Generic;

namespace StrongHelpOfficial.Models
{
    public class AdminUsersViewModel
    {
        public class UserRow
        {
            public int UserID { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
            public string Department { get; set; } = string.Empty;
        }

        public List<UserRow> Users { get; set; } = new();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalUsers { get; set; }
        public int PageSize { get; set; }
        public string? CurrentFilter { get; set; }
        public string? CurrentSearch { get; set; }
    }
}