using Microsoft.AspNetCore.Mvc;
using UniversalDashboard.Models;
using UniversalDashboard.Data;
using Newtonsoft.Json;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;

public class DashboardController : Controller
{
    private readonly AppDbContext _context;

    public DashboardController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(int? siteId)
    {
        var roleLevel = HttpContext.Session.GetInt32("RoleLevel");
        var userSiteId = HttpContext.Session.GetInt32("SiteId");
        var sites = _context.SiteConnections.ToList();
        ViewBag.Sites = sites;

        // Role-based site access
        if (roleLevel == 1)
        {
            if (siteId == null || sites.All(s => s.Id != siteId))
            {
                SetEmptyDashboard();
                return View();
            }
        }
        else if (roleLevel == 2 || roleLevel == 3)
        {
            siteId = userSiteId;
            if (siteId == null || sites.All(s => s.Id != siteId))
            {
                SetEmptyDashboard();
                return View();
            }
        }
        else
        {
            return RedirectToAction("Login", "Auth");
        }

        ViewBag.SelectedSiteId = siteId;
        var site = sites.First(s => s.Id == siteId);

        List<Order> orders = new();
        List<Product> products = new();
        List<Customer> customers = new();

        try
        {
            using var client = new HttpClient();
            client.BaseAddress = new System.Uri(site.ApiUrl);
            if (!string.IsNullOrWhiteSpace(site.ApiKey))
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {site.ApiKey}");

            // Fetch APIs individually; if one fails, log error but continue
            orders = await FetchApiData<List<Order>>(client, "orders") ?? new List<Order>();
            products = await FetchApiData<List<Product>>(client, "products") ?? new List<Product>();
            customers = await FetchApiData<List<Customer>>(client, "customers") ?? new List<Customer>();

            if (!orders.Any()) ViewBag.WarningOrders = "Orders API unavailable or returned no data.";
            if (!products.Any()) ViewBag.WarningProducts = "Products API unavailable or returned no data.";
            if (!customers.Any()) ViewBag.WarningCustomers = "Customers API unavailable or returned no data.";

            // Continue even with partial data
            CalculateDashboardMetrics(orders, products, customers);
        }
        catch (HttpRequestException)
        {
            ViewBag.Error = $"Could not connect to API: {site.ApiUrl}";
            SetEmptyDashboard();
        }
        catch (Exception ex)
        {
            ViewBag.Error = $"Unexpected error: {ex.Message}";
            SetEmptyDashboard();
        }

        return View();
    }

    /// ✅ Fetch API with safe fallback
    private async Task<T?> FetchApiData<T>(HttpClient client, string endpoint)
    {
        try
        {
            var response = await client.GetAsync(endpoint);
            if (!response.IsSuccessStatusCode) return default;

            var content = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(content) || content.TrimStart().StartsWith("<"))
                return default; // HTML error or empty

            return JsonConvert.DeserializeObject<T>(content);
        }
        catch
        {
            return default; // Return null if any failure
        }
    }

    /// ✅ Calculate and populate dashboard metrics using available data
    private void CalculateDashboardMetrics(List<Order> orders, List<Product> products, List<Customer> customers)
    {
        var days = Enumerable.Range(0, 7)
            .Select(i => DateTime.Today.AddDays(-i))
            .OrderBy(d => d)
            .ToArray();

        ViewBag.Dates = days.Select(d => d.ToString("MMM dd")).ToArray();

        // Revenue by day (only if orders are available)
        ViewBag.DailyRevenue = days.Select(day =>
            orders.Where(o => DateTime.TryParse(o.Date, out var d) && d.Date == day).Sum(o => o.Total)
        ).ToArray();

        // Order status breakdown
        ViewBag.OrderStatusLabels = orders.Any()
            ? orders.GroupBy(o => o.Status ?? "Unknown").Select(g => g.Key).ToArray()
            : Array.Empty<string>();

        ViewBag.OrderStatusCounts = orders.Any()
            ? orders.GroupBy(o => o.Status ?? "Unknown").Select(g => g.Count()).ToArray()
            : Array.Empty<int>();

        // Low stock products
        ViewBag.LowStockProducts = products.Where(p => p.Quantity <= 5).ToList();

        // Revenue & Profit
        ViewBag.TotalRevenue = orders.Sum(o => o.Total);
        ViewBag.TotalProfit = CalculateProfit(orders, products);

        // New customers count by day
        var newCustomersByDay = days.Select(day => customers.Count(c => c.Created_Date.Date == day)).ToArray();
        ViewBag.DailyNewCustomers = newCustomersByDay;

        // Pending Shipments & Low Stock counts
        ViewBag.PendingShipments = orders.Count(o => o.Status == "Pending" || o.Status == "Processing");
        ViewBag.LowStock = products.Count(p => p.Quantity <= 5);
    }

    private void SetEmptyDashboard()
    {
        ViewBag.TotalRevenue = 0;
        ViewBag.TotalProfit = 0;
        ViewBag.PendingShipments = 0;
        ViewBag.LowStock = 0;
        ViewBag.DailyRevenue = Array.Empty<decimal>();
        ViewBag.DailyNewCustomers = Array.Empty<int>();
        ViewBag.LowStockProducts = new List<Product>();
        ViewBag.OrderStatusLabels = Array.Empty<string>();
        ViewBag.OrderStatusCounts = Array.Empty<int>();
    }

    private decimal CalculateProfit(List<Order> orders, List<Product> products)
    {
        decimal profit = 0;
        foreach (var order in orders)
        {
            if (order.Products == null) continue;
            foreach (var op in order.Products)
            {
                var product = products.FirstOrDefault(p => p.Id == op.ProductId);
                if (product != null && product.CostPrice != null)
                    profit += (product.Price - product.CostPrice.Value) * op.Quantity;
            }
        }
        return profit;
    }
}
