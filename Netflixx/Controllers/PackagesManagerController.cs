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
                    .ThenInclude(pf => pf.Film)
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

            if (model.SelectedFilmIds != null)
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
    }
}
