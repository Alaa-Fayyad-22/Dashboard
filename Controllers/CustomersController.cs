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
        else if (roleLevel == 2 || roleLevel == 3)
        {
            // Admin/User: always use their assigned site
            siteId = userSiteId;
            if (siteId == null || sites.All(s => s.Id != siteId))
            {
                ViewBag.SelectedSiteId = null;
                return View(new List<Customer>());
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

            // Call the customers endpoint
            var response = await client.GetAsync("customers");

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = $"Failed to fetch data from {site.Name}. (Status: {response.StatusCode})";
                return View(new List<Customer>());
            }

            var json = await response.Content.ReadAsStringAsync();
            var customers = JsonConvert.DeserializeObject<List<Customer>>(json) ?? new List<Customer>();

            if (!customers.Any())
            {
                ViewBag.Error = "No customers found for this site.";
                return View(new List<Customer>());
            }

            return View(customers);
        }
        catch (HttpRequestException)
        {
            ViewBag.Error = $"Could not connect to the API at {site.ApiUrl}. Please check the URL or try again later.";
            return View(new List<Customer>());
        }
        catch (Exception ex)
        {
            ViewBag.Error = $"An unexpected error occurred: {ex.Message}";
            return View(new List<Customer>());
        }
    }
}
