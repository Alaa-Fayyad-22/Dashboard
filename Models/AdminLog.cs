using System;

namespace UniversalDashboard.Models
{
    public class AdminLog
    {
        public int Id { get; set; }
        public int AdminId { get; set; } // The admin user ID
        public string Action { get; set; } // e.g., "CreateUser"
        public string TargetType { get; set; } // "User", "Site", etc.
        public int? TargetId { get; set; }
        public string? Details { get; set; } // JSON/text description
        public DateTime Timestamp { get; set; }
        public string IpAddress { get; set; }
    }
}
