using Microsoft.AspNetCore.Mvc;
using UniversalDashboard.Models;
using UniversalDashboard.Data; // Namespace for your AppDbContext
using System.Linq;

public class SitesController : Controller
{
    private readonly AppDbContext _context;

    public SitesController(AppDbContext context)
    {
        _context = context;
    }

    // GET: /Sites
    public IActionResult Index()
    {

        if (HttpContext.Session.GetInt32("RoleLevel") != 1)
            return Unauthorized(); // Or show an "Access Denied" page

        var sites = _context.SiteConnections.ToList();
        return View(sites);
    }

    // GET: /Sites/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: /Sites/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(SiteConnection site)
    {
        if (ModelState.IsValid)
        {
            _context.SiteConnections.Add(site);
            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }
        return View(site);
    }

    // GET: /Sites/Edit/5
    public IActionResult Edit(int id)
    {
        var site = _context.SiteConnections.Find(id);
        if (site == null) return NotFound();
        return View(site);
    }

    // POST: /Sites/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int id, SiteConnection site)
    {
        if (id != site.Id) return NotFound();
        if (ModelState.IsValid)
        {
            _context.Update(site);
            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }
        return View(site);
    }

    // GET: /Sites/Delete/5
    public IActionResult Delete(int id)
    {
        var site = _context.SiteConnections.Find(id);
        if (site == null) return NotFound();
        return View(site);
    }

    // POST: /Sites/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(int id)
    {
        var site = _context.SiteConnections.Find(id);
        if (site == null) return NotFound();
        _context.SiteConnections.Remove(site);
        _context.SaveChanges();
        return RedirectToAction(nameof(Index));
    }
}
