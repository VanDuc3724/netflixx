using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Netflixx.Models;
using Netflixx.Models.ViewModel;
using Netflixx.Repositories;
using System.Linq;
using System.Threading.Tasks;

namespace Netflixx.Areas.Manager.Controllers
{
    [Area("Manager")]
    [Authorize(Roles = "Manager")]
    public class PackageController : Controller
    {
        private readonly DBContext _context;
        public PackageController(DBContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var packages = await _context.Packages
                .Include(p => p.PackageFilms)
                    .ThenInclude(pf => pf.Film)
                .ToListAsync();

            var viewModel = packages.Select(p => new FilmpackageIndexViewModel
            {
                PackageId = p.Id,
                Name = p.Name,
                Description = p.Description,
                Films = p.PackageFilms.Select(pf => pf.Film.Title).ToList()
            }).ToList();

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var films = await _context.Films
                .Select(f => new FilmOption { Id = f.Id, Title = f.Title })
                .ToListAsync();
            var model = new PackageCreateViewModel
            {
                AvailableFilms = films
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PackageCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AvailableFilms = await _context.Films
                    .Select(f => new FilmOption { Id = f.Id, Title = f.Title })
                    .ToListAsync();
                return View(model);
            }

            var package = new PackagesModel
            {
                Name = model.Name,
                Description = model.Description
            };
            _context.Packages.Add(package);
            await _context.SaveChangesAsync();

            if (model.SelectedFilmIds != null)
            {
                foreach (var filmId in model.SelectedFilmIds)
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
    }
}
