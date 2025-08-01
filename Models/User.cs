// Models/User.cs
namespace UniversalDashboard.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string Email { get; set; }
        public int RoleLevel { get; set; } // 1=SuperAdmin, 2=Admin, 3=User
        public int SiteId { get; set; } // Nullable: only for Admins/Users
        public bool IsActive { get; set; }
    }
}
