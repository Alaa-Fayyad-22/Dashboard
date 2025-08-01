using Microsoft.AspNetCore.Mvc;
using UniversalDashboard.Models;
using UniversalDashboard.Data;
using Newtonsoft.Json;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;

public class ProductsController : Controller
{
    private readonly AppDbContext _context;

    public ProductsController(AppDbContext context)
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
            // SuperAdmin chooses
            if (siteId == null || sites.All(s => s.Id != siteId))
            {
                ViewBag.SelectedSiteId = null;
                return View(new List<Product>());
            }
        }
        else if (roleLevel == 2 || roleLevel == 3)
        {
            // Admin/User: force their assigned site
            siteId = userSiteId;
            if (siteId == null || sites.All(s => s.Id != siteId))
            {
                ViewBag.SelectedSiteId = null;
                return View(new List<Product>());
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

            var response = await client.GetAsync("products");

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = $"Failed to fetch products from {site.Name}. (Status: {response.StatusCode})";
                return View(new List<Product>());
            }

            var json = await response.Content.ReadAsStringAsync();
            var products = JsonConvert.DeserializeObject<List<Product>>(json) ?? new List<Product>();

            if (!products.Any())
            {
                ViewBag.Error = "No products found for this site.";
                return View(new List<Product>());
            }

            return View(products);
        }
        catch (HttpRequestException)
        {
            ViewBag.Error = $"Could not connect to the API at {site.ApiUrl}. Please check the URL or try again later.";
            return View(new List<Product>());
        }
        catch (System.Exception ex)
        {
            ViewBag.Error = $"An unexpected error occurred: {ex.Message}";
            return View(new List<Product>());
        }
    }
}
