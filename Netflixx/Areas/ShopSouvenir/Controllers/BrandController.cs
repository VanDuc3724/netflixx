using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Netflixx.Models;
using Netflixx.Repositories;
using System.Linq;
using System.Threading.Tasks;

namespace Netflixx.Areas.ShopSouvenir.Controllers
{
    [Area("ShopSouvenir")]
    public class BrandController : Controller
    {
        private readonly DBContext _context;

        public BrandController(DBContext context)
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
            if (ModelState.IsValid)
            {
                _context.BrandSous.Add(brand);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(brand);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

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
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(brand);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BrandExists(brand.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(brand);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var brand = await _context.BrandSous
                .FirstOrDefaultAsync(m => m.Id == id);
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
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var brand = await _context.BrandSous.FirstOrDefaultAsync(m => m.Id == id);
            if (brand == null)
            {
                return NotFound();
            }
            return View(brand);
        }

        private bool BrandExists(int id)
        {
            return _context.BrandSous.Any(e => e.Id == id);
        }
    }
}
