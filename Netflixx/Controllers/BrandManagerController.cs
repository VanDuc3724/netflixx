using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Netflixx.Models;
using Netflixx.Repositories;

namespace Netflixx.Controllers
{
    public class BrandManagerController : Controller
    {
        private readonly DBContext _context;

        public BrandManagerController(DBContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var brands = await _context.BrandSous.ToListAsync();
            return View(brands);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BrandSouModel brand)
        {
            if (!ModelState.IsValid)
            {
                return View(brand);
            }

            _context.BrandSous.Add(brand);
            await _context.SaveChangesAsync();
            TempData["success"] = "Brand created successfully";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var brand = await _context.BrandSous.FindAsync(id);
            if (brand == null)
            {
                return NotFound();
            }
            return View(brand);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BrandSouModel brand)
        {
            if (id != brand.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(brand);
            }

            _context.Update(brand);
            await _context.SaveChangesAsync();
            TempData["success"] = "Brand updated successfully";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var brand = await _context.BrandSous.FindAsync(id);
            if (brand == null)
            {
                return NotFound();
            }
            return View(brand);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var brand = await _context.BrandSous.FindAsync(id);
            if (brand != null)
            {
                _context.BrandSous.Remove(brand);
                await _context.SaveChangesAsync();
                TempData["success"] = "Brand deleted successfully";
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int id)
        {
            var brand = await _context.BrandSous.FirstOrDefaultAsync(b => b.Id == id);
            if (brand == null)
            {
                return NotFound();
            }
            return View(brand);
        }
    }
}
