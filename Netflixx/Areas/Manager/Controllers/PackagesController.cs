using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Netflixx.Models;
using Netflixx.Repositories;

namespace Netflixx.Areas.Manager.Controllers
{
    //[Area("Manager")]
    //[Authorize(Roles = "Manager")]
    public class PackagesController : Controller
    {
        private readonly DBContext _context;
        public PackagesController(DBContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var packages = await _context.Packages
                .Include(p => p.PackageFilms)
                .ThenInclude(pf => pf.Film)
                .ToListAsync();
            return View(packages);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Films = await _context.Films.ToListAsync();
            return View(new PackagesModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PackagesModel package, int[] selectedFilms)
        {
            if (ModelState.IsValid)
            {
                _context.Packages.Add(package);
                await _context.SaveChangesAsync();

                if (selectedFilms != null && selectedFilms.Length > 0)
                {
                    foreach (var filmId in selectedFilms)
                    {
                        _context.PackageFilms.Add(new PackageFilmsModel
                        {
                            PackageID = package.Id,
                            FilmID = filmId
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(Index));
            }

            ViewBag.Films = await _context.Films.ToListAsync();
            return View(package);
        }
    }
}
