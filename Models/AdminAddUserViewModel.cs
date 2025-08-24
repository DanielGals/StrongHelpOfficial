using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace StrongHelpOfficial.Models
{
    public class AdminAddUserViewModel
    {
        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = "";

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = "";

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = "";

        [Required]
        [Display(Name = "Contact Number")]
        [RegularExpression(@"^\d+$", ErrorMessage = "Contact number must be numeric.")]
        public string ContactNum { get; set; } = "";

        [Required]
        [Display(Name = "Role")]
        public string RoleId { get; set; } = "";

        [Required]
        [Display(Name = "Department")]
        public string DepartmentId { get; set; } = "";

        [Required]
        [Display(Name = "Date Hired")]
        [DataType(DataType.Date)]
        public DateTime? DateHired { get; set; }

        public List<DropdownItem> Roles { get; set; } = new();
        public List<DropdownItem> Departments { get; set; } = new();

        public string? SuccessMessage { get; set; }
    }

    public class DropdownItem
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
    }
}