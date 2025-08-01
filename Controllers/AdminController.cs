using Microsoft.AspNetCore.Mvc;
using UniversalDashboard.Data;
using UniversalDashboard.Models;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
 // Make sure user is authenticated
public class AdminController : Controller
{
    private readonly AppDbContext _context;

    public AdminController(AppDbContext context)
    {
        _context = context;
    }

    // Simple check for SuperAdmin
    private bool IsSuperAdmin() =>
        HttpContext.Session.GetInt32("RoleLevel") == 1;

    public IActionResult Index()
    {
        if (!IsSuperAdmin()) return Forbid();
        return View();
    }

    // ========== USER MANAGEMENT ==========
    
    public IActionResult Users()
    {
        if (!IsSuperAdmin()) return Forbid();
        var users = _context.Users.ToList();
        return View(users);
    }
    
    public IActionResult CreateUser()
    {
        if (!IsSuperAdmin()) return Forbid();
        ViewBag.Sites = _context.SiteConnections.ToList();
        return View();
    }

    [HttpPost]
    public IActionResult CreateUser(User user)
    {
        if (!IsSuperAdmin()) return Forbid();
        user.IsActive = true;
        _context.Users.Add(user);
        _context.SaveChanges();
        LogAction("CreateUser", "User", user.Id, user);
        return RedirectToAction("Users");
    }
    
    public IActionResult EditUser(int id)
    {
        if (!IsSuperAdmin()) return Forbid();
        var user = _context.Users.FirstOrDefault(u => u.Id == id);
        ViewBag.Sites = _context.SiteConnections.ToList();
        return View(user);
    }

    [HttpPost]
    public IActionResult EditUser(User user)
    {
        if (!IsSuperAdmin()) return Forbid();
        var dbUser = _context.Users.Find(user.Id);
        if (dbUser != null)
        {
            var before = new { dbUser.Username, dbUser.Email, dbUser.RoleLevel, dbUser.SiteId, dbUser.IsActive };
            dbUser.Username = user.Username;
            if (!string.IsNullOrEmpty(user.PasswordHash))
                dbUser.PasswordHash = user.PasswordHash; // hash in prod
            dbUser.Email = user.Email;
            dbUser.RoleLevel = user.RoleLevel;
            dbUser.SiteId = user.SiteId;
            dbUser.IsActive = user.IsActive;
            _context.SaveChanges();
            LogAction("EditUser", "User", user.Id, new { Before = before, After = user });

        }
        return RedirectToAction("Users");
    }

    [HttpPost]
    public IActionResult DeleteUser(int id)
    {
        if (!IsSuperAdmin()) return Forbid();
        var user = _context.Users.Find(id);
        if (user != null)
        {
            _context.Users.Remove(user);
            _context.SaveChanges();
            LogAction("DeleteUser", "User", id, user);
        }
        return RedirectToAction("Users");
    }

    // ========== SITE MANAGEMENT ==========
    //public IActionResult Sites()
    //{
    //    if (!IsSuperAdmin()) return Forbid();
    //    var sites = _context.SiteConnections.ToList();
    //    return View(sites);
    //}

    //public IActionResult CreateSite()
    //{
    //    if (!IsSuperAdmin()) return Forbid();
    //    return View();
    //}

    //[HttpPost]
    //public IActionResult CreateSite(SiteConnection site)
    //{
    //    if (!IsSuperAdmin()) return Forbid();
    //    _context.SiteConnections.Add(site);
    //    _context.SaveChanges();
    //    return RedirectToAction("Sites");
    //}

    //public IActionResult EditSite(int id)
    //{
    //    if (!IsSuperAdmin()) return Forbid();
    //    var site = _context.SiteConnections.Find(id);
    //    return View(site);
    //}

    //[HttpPost]
    //public IActionResult EditSite(SiteConnection site)
    //{
    //    if (!IsSuperAdmin()) return Forbid();
    //    var dbSite = _context.SiteConnections.Find(site.Id);
    //    if (dbSite != null)
    //    {
    //        dbSite.Name = site.Name;
    //        dbSite.ApiUrl = site.ApiUrl;
    //        dbSite.ApiKey = site.ApiKey;
    //        dbSite.IconUrl = site.IconUrl;
    //        _context.SaveChanges();
    //    }
    //    return RedirectToAction("Sites");
    //}

    //[HttpPost]
    //public IActionResult DeleteSite(int id)
    //{
    //    if (!IsSuperAdmin()) return Forbid();
    //    var site = _context.SiteConnections.Find(id);
    //    if (site != null)
    //    {
    //        _context.SiteConnections.Remove(site);
    //        _context.SaveChanges();
    //    }
    //    return RedirectToAction("Sites");
    //}

    public IActionResult AdminLogs()
    {
        if (!IsSuperAdmin()) return Forbid();
        var logs = _context.AdminLogs
            .OrderByDescending(l => l.Timestamp)
            .Take(200)
            .ToList();
        return View(logs);
    }

    // ========== LOGGING UTILITY ==========

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

}
