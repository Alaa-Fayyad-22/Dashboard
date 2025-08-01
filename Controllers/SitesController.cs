using Microsoft.AspNetCore.Mvc;
using UniversalDashboard.Models;
using UniversalDashboard.Data;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using UniversalDashboard.Services;
using System.Text.Json;
using UniversalDashboard.Models.DTOs;

public class SitesController : Controller
{
    private readonly AppDbContext _context;
    private readonly ApiService _apiService;

    public SitesController(AppDbContext context, ApiService apiService)
    {
        _context = context;

        _apiService = apiService;
    }

    private bool IsSuperAdmin() =>
        HttpContext.Session.GetInt32("RoleLevel") == 1;

    // GET: /Sites
    public IActionResult Index()
    {
        if (!IsSuperAdmin()) return Forbid();

        var sites = _context.SiteConnections.ToList();
        return View(sites);
    }

    // GET: /Sites/Create
    public IActionResult Create()
    {
        if (!IsSuperAdmin()) return Forbid();
        return View();
    }

    // POST: /Sites/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(SiteConnection site)
    {
        if (!IsSuperAdmin()) return Forbid();
        Console.WriteLine("POST method hit!");

        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .SelectMany(x => x.Value.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            Console.WriteLine("Validation Errors: " + string.Join(", ", errors));
            return View(site);
        }

        _context.SiteConnections.Add(site);
        _context.SaveChanges();

        // ✅ Log site creation
        LogAction("CreateSite", "Site", site.Id, new { Created = site });

        return RedirectToAction(nameof(Index));
    }

    // GET: /Sites/Edit/5
    public IActionResult Edit(int id)
    {
        if (!IsSuperAdmin()) return Forbid();
        var site = _context.SiteConnections.Find(id);
        if (site == null) return NotFound();
        return View(site);
    }

    // POST: /Sites/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int id, SiteConnection site)
    {
        if (!IsSuperAdmin()) return Forbid();
        if (id != site.Id) return NotFound();

        if (ModelState.IsValid)
        {
            var before = _context.SiteConnections.AsNoTracking().FirstOrDefault(s => s.Id == id);

            _context.Update(site);
            _context.SaveChanges();

            // ✅ Log site edit (Before & After)
            LogAction("EditSite", "Site", site.Id, new { Before = before, After = site });

            return RedirectToAction(nameof(Index));
        }

        return View(site);
    }

    // GET: /Sites/Delete/5
    public IActionResult Delete(int id)
    {
        if (!IsSuperAdmin()) return Forbid();
        var site = _context.SiteConnections.Find(id);
        if (site == null) return NotFound();
        return View(site);
    }

    // POST: /Sites/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(int id)
    {
        if (!IsSuperAdmin()) return Forbid();
        var site = _context.SiteConnections.Find(id);
        if (site == null) return NotFound();

        var before = site;
        _context.SiteConnections.Remove(site);
        _context.SaveChanges();

        // ✅ Log deletion
        LogAction("DeleteSite", "Site", id, new { Deleted = before });

        return RedirectToAction(nameof(Index));
    }

    // Admin Logs Page
    public IActionResult AdminLogs()
    {
        if (!IsSuperAdmin()) return Forbid();
        var logs = _context.AdminLogs
            .OrderByDescending(l => l.Timestamp)
            .Take(200)
            .ToList();
        return View(logs);
    }

    // Logging Utility
    public void LogAction(string action, string targetType, int? targetId, object details = null)
    {
        int? adminId = HttpContext.Session.GetInt32("UserId");
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var log = new AdminLog
        {
            AdminId = adminId ?? 0,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            Details = details != null ? Newtonsoft.Json.JsonConvert.SerializeObject(details) : "",
            Timestamp = DateTime.UtcNow,
            IpAddress = ip
        };
        _context.AdminLogs.Add(log);
        _context.SaveChanges();
    }




    [HttpGet]
    public IActionResult TestApiForm(int id)
    {
        var site = _context.SiteConnections.Find(id);
        if (site == null) return NotFound();
        ViewBag.SiteId = id;
        ViewBag.SiteName = site.Name;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> TestApi(int id)
    {
        var site = _context.SiteConnections.Find(id);
        if (site == null) return NotFound();

        try
        {
            var response = await _apiService.FetchSiteData(site.ApiUrl, site.ApiKey, site.Endpoint ?? "products");
            var customers = JsonSerializer.Deserialize<List<CustomerDto>>(response);

            return View("TestApiResult", customers);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"API Test Failed for {site.Name}: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }




    public async Task<IActionResult> CheckApiPerformance(int id)
    {
        var site = _context.SiteConnections.Find(id);
        if (site == null) return NotFound();

        try
        {
            var endpoint = site.Endpoint ?? "products"; // Hardcoded fallback endpoint
            var url = $"{site.ApiUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var response = await _apiService.FetchSiteData(site.ApiUrl, site.ApiKey, endpoint);
            stopwatch.Stop();

            var responseTime = stopwatch.ElapsedMilliseconds;

            ViewBag.ApiName = site.Name;
            ViewBag.ResponseTime = responseTime;
            ViewBag.Status = "Success";
            ViewBag.DataPreview = response.Length > 200 ? response.Substring(0, 200) + "..." : response; // Preview response

            return View("TestApiResult");
        }
        catch (Exception ex)
        {
            ViewBag.ApiName = site.Name;
            ViewBag.ResponseTime = -1;
            ViewBag.Status = $"Failed: {ex.Message}";
            ViewBag.DataPreview = null;
            return View("TestApiResult");
        }
    }



}
