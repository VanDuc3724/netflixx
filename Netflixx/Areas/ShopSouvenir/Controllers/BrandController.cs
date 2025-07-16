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
            var updateBrand = await _context.BrandSous.FindAsync(brand);

            if (updateBrand == null) // Add this check
            {
                TempData["error"] = "Không tìm thấy thương hiệu";
                return RedirectToAction("Index");
            }

            if (ModelState.IsValid)
            {
                updateBrand.Name = updateBrand.Name;
                updateBrand.Description = updateBrand.Description;

                _context.BrandSous.Update(updateBrand);
                await _context.SaveChangesAsync();
                TempData["success"] = "Chỉnh sửa thương hiệu thành công";
                return RedirectToAction("Index");
            }
            else
            {
                // Return to the Edit view with the model to show validation errors
                return View(brand);
            }
        }

        public async Task<IActionResult> Remove(int BrandId)
        {
            BrandSouModel brand = await _context.BrandSous.FindAsync(BrandId);
            _context.BrandSous.Remove(brand);
            _context.SaveChanges();
            TempData["error"] = "Xóa thương hiệu thành công";
            return RedirectToAction("Index");
        }
    }
}
