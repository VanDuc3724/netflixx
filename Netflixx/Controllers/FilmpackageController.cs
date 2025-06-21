using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
            return View();
        }

        public IActionResult Billhistory()
        {
            return View();
        }

        public IActionResult Buy(int packageId, string packageName, decimal packagePrice)
        {
            var model = new PackagesModel
            {
                Id = packageId,
                Name = packageName,
                Price = packagePrice
            };

            return View(model);
        }

        public async Task<IActionResult> GetPackages()
        {
            var packages = await _context.Packages
                .Select(p => new { id = p.Id, name = p.Name, price = p.Price, description = p.Description })
                .ToListAsync();
            return Json(packages);
        }

    }
}
