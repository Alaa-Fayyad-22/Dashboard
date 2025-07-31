using Microsoft.AspNetCore.Mvc;
using UniversalDashboard.Models;
using UniversalDashboard.Data;
using Newtonsoft.Json;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;

public class CustomersController : Controller
{
    private readonly AppDbContext _context;

    public CustomersController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(int? siteId)
    {
        var roleLevel = HttpContext.Session.GetInt32("RoleLevel");
        var userSiteId = HttpContext.Session.GetInt32("SiteId");
        if (roleLevel == null)
            return RedirectToAction("Login", "Auth");

        var sites = _context.SiteConnections.ToList();
        ViewBag.Sites = sites;

        if (roleLevel == 1)
        {
            // SuperAdmin: can pick any site
            if (siteId == null || sites.All(s => s.Id != siteId))
            {
                ViewBag.SelectedSiteId = null;
                return View(new List<Customer>());
            }
        }
        else
        {
            // Admin/User: always use their assigned site
            siteId = userSiteId;
            if (siteId == null || sites.All(s => s.Id != siteId))
            {
                ViewBag.SelectedSiteId = null;
                return View(new List<Customer>());
            }
        }
        ViewBag.SelectedSiteId = siteId;

        var site = sites.First(s => s.Id == siteId);
        using var client = new HttpClient();
        client.BaseAddress = new System.Uri(site.ApiUrl);
        if (!string.IsNullOrWhiteSpace(site.ApiKey))
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {site.ApiKey}");

        // The endpoint might be "customers", "users", or something else—adjust as needed
        var response = await client.GetAsync("customers");
        var json = await response.Content.ReadAsStringAsync();
        var customers = JsonConvert.DeserializeObject<List<Customer>>(json);

        ViewBag.SelectedSiteId = siteId;
        return View(customers);
    }
}
