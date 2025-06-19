using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Netflixx.Models.Souvenir;
using Netflixx.Repositories;

namespace Netflixx.Areas.ShopSouvenir.Controllers
{
    [Area("ShopSouvenir")]
    public class HomeController : Controller
    {
        private readonly DBContext _context;

        public HomeController(DBContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string category = null)
        {
            IQueryable<ProductSouModel> query = _context.ProductSous
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Series);

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(p => p.Category != null && p.Category.Name.ToLower() == category.ToLower());
            }

            var products = await query.ToListAsync();
            return View(products);
        }
    }
}
