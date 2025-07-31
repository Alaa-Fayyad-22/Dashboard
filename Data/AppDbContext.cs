using Microsoft.EntityFrameworkCore;
using UniversalDashboard.Models;

namespace UniversalDashboard.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<SiteConnection> SiteConnections { get; set; }
        public DbSet<DashboardStats> DashboardStats { get; set; }
        public DbSet<User> Users { get; set; }

    }
}
