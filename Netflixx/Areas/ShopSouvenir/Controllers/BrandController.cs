using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Netflixx.Models;
using Netflixx.Data; // Assuming you have a DbContext

namespace Netflixx.Controllers
{
    public class BrandController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BrandController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Brand
        public async Task<IActionResult> Index()
        {
            var brands = await _context.BrandSouModels
                .Include(b => b.Products)
                .ToListAsync();
            return View(brands);
        }

        // GET: Brand/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var brand = await _context.BrandSouModels
                .Include(b => b.Products)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (brand == null)
            {
                return NotFound();
            }

            return View(brand);
        }

        // GET: Brand/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Brand/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description")] BrandSouModel brand)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(brand);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Brand created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred while creating the brand: " + ex.Message);
                }
            }
            return View(brand);
        }

        // GET: Brand/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var brand = await _context.BrandSouModels.FindAsync(id);
            if (brand == null)
            {
                return NotFound();
            }
            return View(brand);
        }

        // POST: Brand/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description")] BrandSouModel brand)
        {
            if (id != brand.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(brand);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Brand updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BrandExists(brand.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        ModelState.AddModelError("", "The brand was updated by another user. Please try again.");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "An error occurred while updating the brand: " + ex.Message);
                }
            }
            return View(brand);
        }

        // GET: Brand/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var brand = await _context.BrandSouModels
                .Include(b => b.Products)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (brand == null)
            {
                return NotFound();
            }

            return View(brand);
        }

        // POST: Brand/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var brand = await _context.BrandSouModels
                    .Include(b => b.Products)
                    .FirstOrDefaultAsync(b => b.Id == id);

                if (brand == null)
                {
                    return NotFound();
                }

                // Check if brand has products
                if (brand.Products.Any())
                {
                    TempData["ErrorMessage"] = "Cannot delete brand because it has associated products.";
                    return RedirectToAction(nameof(Index));
                }

                _context.BrandSouModels.Remove(brand);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Brand deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while deleting the brand: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        private bool BrandExists(int id)
        {
            return _context.BrandSouModels.Any(e => e.Id == id);
        }

        // API endpoint for getting brands (for dropdowns, etc.)
        [HttpGet]
        public async Task<JsonResult> GetBrandsJson()
        {
            var brands = await _context.BrandSouModels
                .Select(b => new { b.Id, b.Name })
                .ToListAsync();
            return Json(brands);
        }

        // Search functionality
        public async Task<IActionResult> Search(string searchTerm)
        {
            var brands = await _context.BrandSouModels
                .Include(b => b.Products)
                .Where(b => string.IsNullOrEmpty(searchTerm) ||
                           b.Name.Contains(searchTerm) ||
                           b.Description.Contains(searchTerm))
                .ToListAsync();

            ViewBag.SearchTerm = searchTerm;
            return View("Index", brands);
        }
    }
}