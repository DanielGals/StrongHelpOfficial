using System;
using System.Collections.Generic;

namespace StrongHelpOfficial.Models
{
    public class BenefitsAssistantLogsViewModel
    {
        public List<ActivityLogEntry> Logs { get; set; } = new List<ActivityLogEntry>();
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public string FilterBy { get; set; }
        public DateTime? FilterDate { get; set; }
        public List<string> AvailableActions { get; set; } = new List<string>();
    }

    public class ActivityLogEntry
    {
        public int LogID { get; set; }
        public DateTime Timestamp { get; set; }
        public string Action { get; set; }
        public string ApplicationID { get; set; }
        public string Details { get; set; }
        public string Status { get; set; }
        public string UserName { get; set; }
    }
}
