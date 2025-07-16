using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Netflixx.Models;
using Netflixx.Repositories;
using System;
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
        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.BrandSous.Where(b => !b.IsDeleted).AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(b => b.Name.Contains(search));
            }
            ViewData["CurrentFilter"] = search;
            var list = await query.AsNoTracking().ToListAsync();
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _context.BrandSous
                .Where(b => !b.IsDeleted)
                .AsNoTracking()
                .Select(b => new
                {
                    b.Id,
                    b.Name,
                    b.Description
                })
                .ToListAsync();

            return Json(list);
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
            if (_context.BrandSous.Any(b => b.Name == brand.Name))
            {
                ModelState.AddModelError("Name", "Tên thương hiệu đã tồn tại");
            }

            if (ModelState.IsValid)
            {
                brand.CreatedAt = DateTime.Now;
                brand.UpdatedAt = DateTime.Now;
                _context.Add(brand);
                await _context.SaveChangesAsync();

                _context.BrandHistories.Add(new BrandHistory
                {
                    BrandId = brand.Id,
                    BrandName = brand.Name,
                    Action = "Create",
                    Details = $"Created {brand.Name}",
                    Timestamp = DateTime.Now
                });
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

            if (_context.BrandSous.Any(b => b.Name == brand.Name && b.Id != brand.Id))
            {
                ModelState.AddModelError("Name", "Tên thương hiệu đã tồn tại");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var original = await _context.BrandSous.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
                    brand.UpdatedAt = DateTime.Now;
                    _context.Update(brand);
                    await _context.SaveChangesAsync();

                    var changes = new System.Collections.Generic.List<string>();
                    if (original != null)
                    {
                        if (original.Name != brand.Name)
                            changes.Add($"Name: '{original.Name}' -> '{brand.Name}'");
                        if (original.Description != brand.Description)
                            changes.Add("Description updated");
                    }
                    var details = string.Join("; ", changes);
                    _context.BrandHistories.Add(new BrandHistory
                    {
                        BrandId = brand.Id,
                        BrandName = brand.Name,
                        Action = "Edit",
                        Details = details,
                        Timestamp = DateTime.Now
                    });
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
                brand.IsDeleted = true;
                brand.DeletedAt = DateTime.Now;
                _context.Update(brand);
                _context.BrandHistories.Add(new BrandHistory
                {
                    BrandId = brand.Id,
                    BrandName = brand.Name,
                    Action = "Delete",
                    Details = "Moved to trash",
                    Timestamp = DateTime.Now
                });
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã chuyển vào thùng rác!";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool BrandExists(int id)
        {
            return _context.BrandSous.Any(e => e.Id == id);
        }

        [HttpGet]
        public async Task<IActionResult> Trash()
        {
            var list = await _context.BrandSous
                .Where(b => b.IsDeleted)
                .OrderByDescending(b => b.DeletedAt)
                .ToListAsync();
            return View("Trash", list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(int id)
        {
            var brand = await _context.BrandSous.FindAsync(id);
            if (brand != null)
            {
                brand.IsDeleted = false;
                brand.DeletedAt = null;
                _context.Update(brand);
                _context.BrandHistories.Add(new BrandHistory
                {
                    BrandId = brand.Id,
                    BrandName = brand.Name,
                    Action = "Restore",
                    Details = "Restored from trash",
                    Timestamp = DateTime.Now
                });
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã khôi phục thành công!";
            }
            return RedirectToAction(nameof(Trash));
        }

        [HttpGet]
        public async Task<IActionResult> HardDelete(int? id)
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
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("HardDelete", brand);
            }
            return View("HardDelete", brand);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HardDelete(int id)
        {
            var brand = await _context.BrandSous.FindAsync(id);
            if (brand != null)
            {
                _context.BrandHistories.Add(new BrandHistory
                {
                    BrandId = brand.Id,
                    BrandName = brand.Name,
                    Action = "HardDelete",
                    Details = "Permanent delete",
                    Timestamp = DateTime.Now
                });
                _context.BrandSous.Remove(brand);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa vĩnh viễn!";
            }
            return RedirectToAction(nameof(Trash));
        }

        [HttpGet]
        public async Task<IActionResult> History(int id)
        {
            var list = await _context.BrandHistories
                .Where(h => h.BrandId == id)
                .OrderByDescending(h => h.Timestamp)
                .ToListAsync();
            ViewBag.BrandId = id;
            return View("History", list);
        }

        [HttpGet]
        public async Task<IActionResult> HistoryAll()
        {
            var list = await _context.BrandHistories
                .Include(h => h.Brand)
                .OrderByDescending(h => h.Timestamp)
                .ToListAsync();
            return View("HistoryAll", list);
        }

        [HttpGet]
        public async Task<IActionResult> HistoryDetails(int id, string returnTo = null)
        {
            var history = await _context.BrandHistories
                .Include(h => h.Brand)
                .FirstOrDefaultAsync(h => h.Id == id);
            if (history == null)
            {
                return NotFound();
            }
            ViewBag.ReturnTo = returnTo;
            return View("HistoryDetails", history);
        }
    }
}
