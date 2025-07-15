using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Netflixx.Models;
using Netflixx.Models.ViewModel;
using Netflixx.Repositories;
using System.Linq;
using System.Threading.Tasks;

namespace Netflixx.Controllers
{
    public class FilmpackageController : Controller
    {
        private readonly DBContext _context;

        public FilmpackageController(DBContext context)
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

        public IActionResult Billhistory()
        {
            return View();
        }

        public IActionResult Buy(string packageId, string packageName, int packagePrice)
        {
            var model = new FilmPackageViewModel
            {
                PackageId = packageId,
                PackageName = packageName,
                PackagePrice = packagePrice
            };

            return View(model);
        }

    }
}
