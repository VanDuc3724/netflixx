using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Netflixx.Models;
using Netflixx.Repositories;
using System.Linq;
using System.Threading.Tasks;

namespace Netflixx.Controllers
{
    public class PackagesController : Controller
    {
        private readonly DBContext _context;
        public PackagesController(DBContext context)
        {
            _context = context;
        }

    public async Task<IActionResult> Index()
    {
        var packages = await _context.Packages.ToListAsync();
        return View(packages);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PackagesModel model)
    {
        if (ModelState.IsValid)
        {
            _context.Packages.Add(model);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var pkg = await _context.Packages.FindAsync(id);
        if (pkg != null)
        {
            _context.Packages.Remove(pkg);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var pkg = await _context.Packages.FindAsync(id);
        if (pkg == null)
        {
            return NotFound();
        }

        return View(pkg);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PackagesModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(model);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PackageExists(model.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }
        return View(model);
    }

    private bool PackageExists(int id)
    {
        return _context.Packages.Any(e => e.Id == id);
    }
}
}
