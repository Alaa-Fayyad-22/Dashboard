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
        else if (roleLevel == 2 || roleLevel == 3)
        {
            // Admin/User: only their site
            siteId = userSiteId;
            if (siteId == null || sites.All(s => s.Id != siteId))
            {
                ViewBag.SelectedSiteId = null;
                return View(new List<Order>());
            }
        }
        else
        {
            return RedirectToAction("Login", "Auth");
        }
        ViewBag.SelectedSiteId = siteId;

        var site = sites.First(s => s.Id == siteId);
        try
        {
            using var client = new HttpClient();
            client.BaseAddress = new System.Uri(site.ApiUrl);
            if (!string.IsNullOrWhiteSpace(site.ApiKey))
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {site.ApiKey}");

            // Fetch orders
            var response = await client.GetAsync("orders");
            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = $"Failed to fetch orders from {site.Name}. (Status: {response.StatusCode})";
                return View(new List<Order>());
            }

            var json = await response.Content.ReadAsStringAsync();
            var orders = JsonConvert.DeserializeObject<List<Order>>(json) ?? new List<Order>();

            if (!orders.Any())
            {
                ViewBag.Error = "No orders found for this site.";
                return View(new List<Order>());
            }

            // Fetch products
            var productsResponse = await client.GetAsync("products");
            if (!productsResponse.IsSuccessStatusCode)
            {
                ViewBag.Error = $"Orders loaded, but failed to fetch products. (Status: {productsResponse.StatusCode})";
                ViewBag.Products = new List<Product>();
            }
            else
            {
                var productsJson = await productsResponse.Content.ReadAsStringAsync();
                ViewBag.Products = JsonConvert.DeserializeObject<List<Product>>(productsJson) ?? new List<Product>();
            }

            // Calculate totals
            ViewBag.TotalRevenue = orders.Sum(o => o.Total);
            ViewBag.TotalOrders = orders.Count;

            decimal profit = 0;
            decimal costprice = 0;

            if (ViewBag.Products is List<Product> products)
            {
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
            }

            ViewBag.TotalProfit = profit;
            ViewBag.CostPrice = costprice;

            return View(orders);
        }
        catch (HttpRequestException)
        {
            ViewBag.Error = $"Could not connect to the API at {site.ApiUrl}. Please check the URL or try again later.";
            return View(new List<Order>());
        }
        catch (System.Exception ex)
        {
            ViewBag.Error = $"An unexpected error occurred: {ex.Message}";
            return View(new List<Order>());
        }
    }
}
