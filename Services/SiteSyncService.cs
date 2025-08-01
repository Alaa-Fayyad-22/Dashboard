using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UniversalDashboard.Data;
using UniversalDashboard.Services;

namespace UniversalDashboard.Services
{
    public class SiteSyncService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public SiteSyncService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var apiService = scope.ServiceProvider.GetRequiredService<ApiService>();

                var sites = db.SiteConnections.ToList();

                foreach (var site in sites)
                {
                    try
                    {
                        var data = await apiService.FetchSiteData(site.ApiUrl, site.ApiKey, "ping");
                        Console.WriteLine($"[SYNC] {site.Name} responded: {data}");
                        // TODO: Parse/store fetched data in DB if needed.
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[SYNC ERROR] {site.Name}: {ex.Message}");
                    }
                }

                await Task.Delay(TimeSpan.FromHours(1), stoppingToken); // Sync hourly
            }
        }
    }
}
