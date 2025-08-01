using Microsoft.AspNetCore.Mvc;
using UniversalDashboard.Models;
using UniversalDashboard.Data;
using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

public class AuthController : Controller
{
    private readonly AppDbContext _context;

    public AuthController(AppDbContext context)
    {
        _context = context;
    }

    // LOGGING FUNCTION (uses DI _context so you don't need a new AppDbContext instance)
    public void LogAction(string action, string targetType, int? targetId, object details = null)
    {
        int? userId = HttpContext.Session.GetInt32("UserId");
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var log = new AdminLog
        {
            AdminId = userId ?? 0,
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
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Login(string username, string password)
    {
        // Trim the input username
        var inputUsername = username.Trim();
        

        // Case-insensitive lookup using PostgreSQL ILIKE
        var user = _context.Users
            .FirstOrDefault(u => EF.Functions.ILike(u.Username.Trim(), inputUsername));


        if (user != null && user.PasswordHash == password && user.IsActive) // Use hashing in prod!
        {
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetInt32("RoleLevel", user.RoleLevel);
            HttpContext.Session.SetString("Username", user.Username);

            if (user.RoleLevel > 1 && user.SiteId > 0)
                HttpContext.Session.SetInt32("SiteId", user.SiteId);

            // Log successful login
            LogAction("LoginSuccess", "User", user.Id, new { Username = username });

            return RedirectToAction("Index", "Dashboard");
        }
        else if (user != null && !user.IsActive)
        {
            ViewBag.Error = "Your account is inactive. Please contact alaafayyadp1@gmail.com.";
            // Log attempted login for inactive user
            LogAction("LoginAttemptInactive", "User", user.Id, new { Username = username });

            return View();
        }
        else
        {
            ViewBag.Error = "Invalid login credentials.";
            // Log failed login
            LogAction("LoginFailed", "User", null, new { Username = username, TriedPassword = password });
            return View();
        }
    }

    public IActionResult Logout()
    {
        // Log logout event
        LogAction("Logout", "User", HttpContext.Session.GetInt32("UserId"), null);

        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }
}
