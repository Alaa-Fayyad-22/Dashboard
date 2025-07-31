namespace UniversalDashboard.Models
{
   public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string PasswordHash { get; set; }
    public int RoleLevel { get; set; }
    public int SiteId { get; set; } // The site this user belongs to (if role 2 or 3)
}
}
