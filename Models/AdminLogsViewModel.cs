using System;
using System.Collections.Generic;

namespace StrongHelpOfficial.Models
{
    public class AdminLogEntryViewModel
    {
        public string EntityType { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsCreation { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string? ModifiedBy { get; set; }
    }

    public class AdminLogsViewModel
    {
        public List<AdminLogEntryViewModel> Logs { get; set; } = new();
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalLogs { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalLogs / PageSize);
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
