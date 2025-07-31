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

        if (roleLevel == 1)
        {
            // SuperAdmin: can pick any site
            if (siteId == null || sites.All(s => s.Id != siteId))
            {
                ViewBag.SelectedSiteId = null;
                // Show empty dashboard
                ViewBag.TotalRevenue = 0;
                ViewBag.TotalProfit = 0;
                ViewBag.PendingShipments = 0;
                ViewBag.LowStock = 0;
                return View();
            }
        }
        else if (roleLevel == 2 || roleLevel == 3)
        {
            // Admin/User: must use their assigned site, ignore query param
            siteId = userSiteId;
            if (siteId == null || sites.All(s => s.Id != siteId))
            {
                ViewBag.SelectedSiteId = null;
                // Show empty dashboard
                ViewBag.TotalRevenue = 0;
                ViewBag.TotalProfit = 0;
                ViewBag.PendingShipments = 0;
                ViewBag.LowStock = 0;
                return View();
            }
        }
        else {
            return RedirectToAction("Login", "Auth");
        }

                ViewBag.SelectedSiteId = siteId;

        var site = sites.First(s => s.Id == siteId);
        using var client = new HttpClient();
        client.BaseAddress = new System.Uri(site.ApiUrl);
        if (!string.IsNullOrWhiteSpace(site.ApiKey))
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {site.ApiKey}");

        // Fetch orders
        var ordersResponse = await client.GetAsync("orders");
        var ordersJson = await ordersResponse.Content.ReadAsStringAsync();
        var orders = JsonConvert.DeserializeObject<List<Order>>(ordersJson);

        // Fetch products
        var productsResponse = await client.GetAsync("products");
        var productsJson = await productsResponse.Content.ReadAsStringAsync();
        var products = JsonConvert.DeserializeObject<List<Product>>(productsJson);

        var days = Enumerable.Range(0, 7)
        .Select(i => DateTime.Today.AddDays(-i))
        .OrderBy(d => d)
        .ToArray();

        ViewBag.Dates = days.Select(d => d.ToString("MMM dd")).ToArray();
        var revenueByDay = days.Select(day =>
            orders.Where(o => DateTime.TryParse(o.Date, out var d) && d.Date == day)
                  .Sum(o => o.Total)
        ).ToArray();


        ViewBag.Dates = days.Select(d => d.ToString("MMM dd")).ToArray();
        ViewBag.DailyRevenue = revenueByDay;

        // Calculate Revenue & Profit
        decimal revenue = orders.Sum(o => o.Total);

        var statusGroups = orders.GroupBy(o => o.Status ?? "Unknown")
    .Select(g => new { Status = g.Key, Count = g.Count() })
    .ToList();
        ViewBag.OrderStatusLabels = statusGroups.Select(g => g.Status).ToArray();
        ViewBag.OrderStatusCounts = statusGroups.Select(g => g.Count).ToArray();
        ViewBag.LowStockProducts = products.Where(p => p.Quantity <= 5).ToList();



        // For profit, suppose each Product has a CostPrice property (otherwise skip profit calculation)
        decimal profit = 0;
        foreach (var order in orders)
        {
            if (order.Products == null) continue;
            foreach (var op in order.Products)
            {
                var product = products.FirstOrDefault(p => p.Id == op.ProductId);
                if (product != null && product.CostPrice != null)
                {
                    profit += ((product.Price - product.CostPrice.Value) * op.Quantity);
                }
            }
        }


        var customersResponse = await client.GetAsync("customers");
        var customersJson = await customersResponse.Content.ReadAsStringAsync();
        var customers = JsonConvert.DeserializeObject<List<Customer>>(customersJson);

        // Defensive: ensure no nulls
        if (customers == null)
            customers = new List<Customer>();

        var newCustomersByDay = days.Select(day =>
            customers.Count(c => c.Created_Date.Date == day)
        ).ToArray();

        ViewBag.DailyNewCustomers = newCustomersByDay;

        // Pending Shipments: suppose you have an "OrderStatus" field
        int pendingShipments = orders.Count(o => o.Status == "Pending" || o.Status == "Processing");

        // Low stock: products with quantity <= 5
        int lowStock = products.Count(p => p.Quantity <= 5);

        // Expose data to the view
        ViewBag.TotalRevenue = revenue;
        ViewBag.TotalProfit = profit;
        ViewBag.PendingShipments = pendingShipments;
        ViewBag.LowStock = lowStock;
        ViewBag.SelectedSiteId = siteId;

        return View();
    }
}
