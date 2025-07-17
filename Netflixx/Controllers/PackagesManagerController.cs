using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Netflixx.Models;
using Netflixx.Models.ViewModel;
using Netflixx.Repositories;

namespace Netflixx.Controllers
{
    public class PackagesManagerController : Controller
    {
        private readonly DBContext _context;

        public PackagesManagerController(DBContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var packages = await _context.Packages
                .Include(p => p.PackageFilms)
                .OrderBy(p => p.Name)
                .ToListAsync();
            return View(packages);
        }

        public async Task<IActionResult> Create()
        {
            var vm = new CreatePackageViewModel
            {
                Package = new PackagesModel(),
                Films = await _context.Films.OrderBy(f => f.Title).ToListAsync()
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreatePackageViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Films = await _context.Films.OrderBy(f => f.Title).ToListAsync();
                return View(model);
            }

            _context.Packages.Add(model.Package);
            await _context.SaveChangesAsync();

            if (model.SelectedFilmIds != null && model.SelectedFilmIds.Any())
            {
                foreach (var filmId in model.SelectedFilmIds)
                {
                    _context.PackageFilms.Add(new PackageFilmsModel
                    {
                        PackageID = model.Package.Id,
                        FilmID = filmId
                    });
                }
                await _context.SaveChangesAsync();
            }

            TempData["success"] = "Tạo gói phim thành công!";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var package = await _context.Packages
                .Include(p => p.PackageFilms)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (package == null)
            {
                return NotFound();
            }

            var vm = new CreatePackageViewModel
            {
                Package = package,
                SelectedFilmIds = package.PackageFilms.Select(pf => pf.FilmID).ToList(),
                Films = await _context.Films.OrderBy(f => f.Title).ToListAsync()
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CreatePackageViewModel model)
        {
            if (id != model.Package.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                model.Films = await _context.Films.OrderBy(f => f.Title).ToListAsync();
                return View(model);
            }

            _context.Update(model.Package);
            await _context.SaveChangesAsync();

            var existingFilms = await _context.PackageFilms
                .Where(pf => pf.PackageID == id)
                .ToListAsync();
            _context.PackageFilms.RemoveRange(existingFilms);
            await _context.SaveChangesAsync();

            if (model.SelectedFilmIds != null && model.SelectedFilmIds.Any())
            {
                foreach (var filmId in model.SelectedFilmIds)
                {
                    _context.PackageFilms.Add(new PackageFilmsModel
                    {
                        PackageID = id,
                        FilmID = filmId
                    });
                }
                await _context.SaveChangesAsync();
            }

            TempData["success"] = "Cập nhật gói phim thành công!";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var package = await _context.Packages.FindAsync(id);
            if (package == null)
            {
                return NotFound();
            }
            return View(package);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var package = await _context.Packages.FindAsync(id);
            if (package != null)
            {
                var films = _context.PackageFilms.Where(pf => pf.PackageID == id);
                _context.PackageFilms.RemoveRange(films);
                _context.Packages.Remove(package);
                await _context.SaveChangesAsync();
                TempData["success"] = $"Package '{package.Name}' deleted successfully";
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int id)
        {
            var package = await _context.Packages
                .Include(p => p.PackageFilms)
                .ThenInclude(pf => pf.Film)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (package == null)
            {
                return NotFound();
            }
            return View(package);
        }
    }
}
