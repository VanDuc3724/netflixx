using Microsoft.AspNetCore.Mvc;
using Netflixx.Models;
using Netflixx.Repositories;

namespace Netflixx.Controllers
{
    public class FilmpackageController : Controller
    {
        private readonly DBContext _context;
        public FilmpackageController(DBContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var packages = _context.Packages.ToList();
            return View(packages);
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
