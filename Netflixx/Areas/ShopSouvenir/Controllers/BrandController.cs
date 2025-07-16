using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Netflixx.Models;
using Netflixx.Repositories;

namespace Netflixx.Areas.ShopSouvenir.Controllers
{
    [Area("ShopSouvenir")]
    public class BrandController : Controller
    {
        private readonly DBContext _context;
        private readonly ILogger<BrandController> _logger;

        public BrandController(DBContext context, ILogger<BrandController> logger)
        {
            _context = context;
            _logger = logger;
        }
        public async Task<IActionResult> Index()
        {
            return View(_context.BrandSous.ToList());
        }

        [HttpGet]
        public IActionResult Add()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add([FromForm] BrandSouModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                _context.BrandSous.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error adding brand");
                ModelState.AddModelError("", "Unable to save. Try again.");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int BrandId)
        {
            var brand = await _context.BrandSous.FindAsync(BrandId);
            return View(brand);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int BrandId, BrandSouModel brand)
        {
            if (!ModelState.IsValid)
            {
                return View(brand);
            }

            var updateBrand = await _context.BrandSous.FindAsync(BrandId);

            if (updateBrand == null)
            {
                TempData["error"] = "Không tìm thấy thương hiệu";
                return RedirectToAction("Index");
            }

            updateBrand.Name = brand.Name;
            updateBrand.Description = brand.Description;

            _context.BrandSous.Update(updateBrand);
            await _context.SaveChangesAsync();
            TempData["success"] = "Chỉnh sửa thương hiệu thành công";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int BrandId)
        {
            var brand = await _context.BrandSous.FindAsync(BrandId);
            if (brand != null)
            {
                _context.BrandSous.Remove(brand);
                await _context.SaveChangesAsync();
                TempData["success"] = "Xóa thương hiệu thành công";
            }
            else
            {
                TempData["error"] = "Không tìm thấy thương hiệu";
            }
            return RedirectToAction("Index");
        }
    }
}
