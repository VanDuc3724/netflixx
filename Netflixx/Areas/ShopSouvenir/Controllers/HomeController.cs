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
        public async Task<IActionResult> Index()
        {
            var products = await _context.ProductSous
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Series)
                .ToListAsync();

            return View(products);
        }

        [HttpGet]
        public async Task<IActionResult> QuickView(int id)
        {
            try
            {
                // Đảm bảo bạn có DBContext trong HomeController
                var product = await _context.ProductSous
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Include(p => p.Series)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (product == null)
                {
                    return Json(new { success = false, message = "Sản phẩm không tồn tại" });
                }

                return Json(new
                {
                    success = true,
                    id = product.Id,
                    name = product.Name,
                    price = product.Price,
                    description = product.Description ?? "Không có mô tả",
                    imageUrl = !string.IsNullOrEmpty(product.ImageUrl)
                        ? product.ImageUrl
                        : "/images/default-product.jpg",
                    category = product.Category?.Name ?? "Không có danh mục",
                    brand = product.Brand?.Name ?? "Không có thương hiệu",
                    series = product.Series?.Name ?? "Không có series",
                    stock = product.StockQuantity
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }
    }
}