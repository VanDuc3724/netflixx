using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Netflixx.Areas.ShopSouvenir.Models;
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

        public async Task<IActionResult> Details(int Id)
        {
            if (Id <= 0) return RedirectToAction("Index");

            var productDetails = await _context.ProductSous
                .Include(p => p.ProductImages) // Thêm dòng này để load cả ảnh sản phẩm
                .FirstOrDefaultAsync(c => c.Id == Id);

            if (productDetails == null) return RedirectToAction("Index");

            // Lấy ảnh mặc định (nếu có)
            var defaultImage = productDetails.ProductImages.FirstOrDefault(i => i.IsDefault == true) ??
                              productDetails.ProductImages.FirstOrDefault();

            // Gán ảnh mặc định vào ImageUrl nếu chưa có
            if (defaultImage != null && string.IsNullOrEmpty(productDetails.ImageUrl))
            {
                productDetails.ImageUrl = defaultImage.ImageUrl;
            }

            // Sản phẩm liên quan
            var relatedProducts = await _context.ProductSous
                .Where(c => c.CategoryId == productDetails.CategoryId && c.Id != Id)
                .OrderByDescending(c => c.Id)
                .Take(4)
                .ToListAsync();

            ViewBag.RelatedProducts = relatedProducts;

            var viewModel = new ProductDetailsViewModel
            {
                Product = productDetails,
                ProductImages = productDetails.ProductImages.OrderBy(i => i.DisplayOrder).ToList()
            };

            return View(viewModel);
        }


    }
}
