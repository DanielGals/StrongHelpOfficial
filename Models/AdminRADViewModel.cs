namespace StrongHelpOfficial.Models
{
    public class AdminRoleDto
    {
        public int RoleID { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class AdminDepartmentDto
    {
        public int DepartmentID { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class AdminRADViewModel
    {
        public List<AdminRoleDto> Roles { get; set; } = new List<AdminRoleDto>();
        public List<AdminDepartmentDto> Departments { get; set; } = new List<AdminDepartmentDto>();

    }
}
