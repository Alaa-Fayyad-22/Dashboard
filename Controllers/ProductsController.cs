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
        using var client = new HttpClient();
        client.BaseAddress = new System.Uri(site.ApiUrl);
        if (!string.IsNullOrWhiteSpace(site.ApiKey))
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {site.ApiKey}");

        var response = await client.GetAsync("products");
        var json = await response.Content.ReadAsStringAsync();
        var products = JsonConvert.DeserializeObject<List<Product>>(json);

        ViewBag.SelectedSiteId = siteId;
        return View(products);
    }
}
