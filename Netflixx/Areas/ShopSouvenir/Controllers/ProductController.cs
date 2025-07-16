using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Netflixx.Models.Souvenir;
using Netflixx.Repositories;

namespace Netflixx.Areas.ShopSouvenir.Controllers
{
    [Area("ShopSouvenir")]
    public class ProductController : Controller
    {
        private readonly DBContext _context;

        public ProductController(DBContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string category = null)
        {
            ViewBag.CurrentCategory = category?.ToLower();
            var products = await _context.ProductSous
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Series)
                .ToListAsync();
            return View(products);
        }
    }
}
