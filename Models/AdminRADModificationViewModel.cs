using System;
using System.ComponentModel.DataAnnotations;

namespace StrongHelpOfficial.Models
{
    public class AdminRADModificationViewModel
    {
        public int Id { get; set; }
        public string Context { get; set; } = "Role";

        [Required(ErrorMessage = "Name is required.")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime? ModifiedAt { get; set; }
        public string? ModifiedBy { get; set; }
        public bool IsActive { get; set; }
        public bool EditMode { get; set; } = false;
        public bool ShowSuccess { get; set; } = false;
    }
}
