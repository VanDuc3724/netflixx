using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Netflixx.Models;
using Netflixx.Repositories;
using System.Linq;
using System.Threading.Tasks;

namespace Netflixx.Areas.ShopSouvenir.Controllers
{
    [Area("ShopSouvenir")]
    public class BrandManagerController : Controller
    {
        private readonly DBContext _context;

        public BrandManagerController(DBContext context)
        {
            _context = context;
        }

        // GET: ShopSouvenir/BrandManager
        public async Task<IActionResult> Index()
        {
            var list = await _context.BrandSous.AsNoTracking().ToListAsync();
            return View(list);
        }

        // GET: ShopSouvenir/BrandManager/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var brand = await _context.BrandSous.FirstOrDefaultAsync(b => b.Id == id);
            if (brand == null)
            {
                return NotFound();
            }
            return View(brand);
        }

        // GET: ShopSouvenir/BrandManager/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: ShopSouvenir/BrandManager/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Description")] BrandSouModel brand)
        {
            if (ModelState.IsValid)
            {
                _context.Add(brand);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm thương hiệu thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(brand);
        }

        // GET: ShopSouvenir/BrandManager/Edit/5
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

        // POST: ShopSouvenir/BrandManager/Edit/5
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
                    TempData["SuccessMessage"] = "Cập nhật thành công!";
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

        // GET: ShopSouvenir/BrandManager/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var brand = await _context.BrandSous.FirstOrDefaultAsync(b => b.Id == id);
            if (brand == null)
            {
                return NotFound();
            }
            return View(brand);
        }

        // POST: ShopSouvenir/BrandManager/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var brand = await _context.BrandSous.FindAsync(id);
            if (brand != null)
            {
                _context.BrandSous.Remove(brand);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool BrandExists(int id)
        {
            return _context.BrandSous.Any(e => e.Id == id);
        }
    }
}
