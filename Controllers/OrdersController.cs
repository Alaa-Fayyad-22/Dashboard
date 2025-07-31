using Microsoft.AspNetCore.Mvc;
using UniversalDashboard.Models;
using UniversalDashboard.Data;
using Newtonsoft.Json;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;

public class OrdersController : Controller
{
    private readonly AppDbContext _context;

    public OrdersController(AppDbContext context)
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
                return View(new List<Order>());
            }
        }
        else
        {
            // Admin/User: only their site
            siteId = userSiteId;
            if (siteId == null || sites.All(s => s.Id != siteId))
            {
                ViewBag.SelectedSiteId = null;
                return View(new List<Order>());
            }
        }
        ViewBag.SelectedSiteId = siteId;

        var site = sites.First(s => s.Id == siteId);
        using var client = new HttpClient();
        client.BaseAddress = new System.Uri(site.ApiUrl);
        if (!string.IsNullOrWhiteSpace(site.ApiKey))
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {site.ApiKey}");

        var response = await client.GetAsync("orders");
        var json = await response.Content.ReadAsStringAsync();
        var orders = JsonConvert.DeserializeObject<List<Order>>(json);

        // Fetch products from the API
        var productsResponse = await client.GetAsync("products");
        var productsJson = await productsResponse.Content.ReadAsStringAsync();
        var products = JsonConvert.DeserializeObject<List<Product>>(productsJson);

        ViewBag.Products = products;

        ViewBag.TotalRevenue = orders.Sum(o => o.Total);
        ViewBag.TotalOrders = orders.Count;
        decimal profit = 0;
        decimal costprice = 0;
        foreach (var order in orders)
        {
            if (order.Products == null) continue;
            foreach (var op in order.Products)
            {
                var product = products.FirstOrDefault(p => p.Id == op.ProductId);
                if (product != null && product.CostPrice != null)
                {
                    profit += (product.Price - product.CostPrice.Value) * op.Quantity;
                    costprice = product.CostPrice.Value;
                }
            }
        }

        ViewBag.TotalProfit = profit;
        ViewBag.costprice = costprice;
        


        ViewBag.SelectedSiteId = siteId;
        return View(orders);
    }
}
