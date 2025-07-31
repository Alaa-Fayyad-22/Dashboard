using Microsoft.AspNetCore.Mvc;
using UniversalDashboard.Models;
using UniversalDashboard.Data;
using System.Linq;
using Microsoft.AspNetCore.Http;

public class AuthController : Controller
{
    private readonly AppDbContext _context;

    public AuthController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Login(string username, string password)
    {
        var user = _context.Users.SingleOrDefault(u => u.Username == username);

        if (user != null && user.PasswordHash == password) // TODO: use hashing!
        {
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetInt32("RoleLevel", user.RoleLevel);
            HttpContext.Session.SetString("Username", user.Username);

            if (user.RoleLevel > 1 && user.SiteId > 0)
            {
                // Only store SiteId if user is Admin or Normal User
                HttpContext.Session.SetInt32("SiteId", user.SiteId);
            }

            return RedirectToAction("Index", "Dashboard");
        }

        ViewBag.Error = "Invalid login";
        return View();
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }
}
