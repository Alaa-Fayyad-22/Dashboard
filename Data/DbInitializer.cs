using System.Linq;
using UniversalDashboard.Data;
using UniversalDashboard.Models;

public static class DbInitializer
{
    public static void Initialize(AppDbContext context)
    {
        if (!context.Users.Any())
        {
            // Demo: hash password using BCrypt.Net or another secure hash in production!
            var superAdmin = new User
            {
                Username = "superadmin",
                PasswordHash = "superpassword", // TODO: hash this!
                RoleLevel = 1
            };
            context.Users.Add(superAdmin);
            context.SaveChanges();
        }
    }
}
