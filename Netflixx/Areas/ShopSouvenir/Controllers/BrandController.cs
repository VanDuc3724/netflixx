using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Netflixx.Models;
using System;
using Netflixx.Repositories;
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

        // GET: ShopSouvenir/Brand
        public async Task<IActionResult> Index()
        {
            var brands = await _context.BrandSous.ToListAsync();
            return View(brands);
        }

        // GET: ShopSouvenir/Brand/Details/5
        public async Task<IActionResult> Details(int? id)
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

        // GET: ShopSouvenir/Brand/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: ShopSouvenir/Brand/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Description,Country,Website,EstablishedDate,Alias,CEO,Headquarters,LogoFile,LogoUrl")] BrandSouModel brand)
        {
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
                return RedirectToAction(nameof(Index));
            }
            return View(brand);
        }

        // GET: ShopSouvenir/Brand/Edit/5
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

        // POST: ShopSouvenir/Brand/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Country,Website,EstablishedDate,Alias,CEO,Headquarters,LogoFile,LogoUrl,CreatedAt")] BrandSouModel brand)
        {
            if (id != brand.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var original = await _context.BrandSous.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
                    brand.UpdatedAt = DateTime.Now;
                    _context.Update(brand);
                    await _context.SaveChangesAsync();

                    var changes = new List<string>();
                    if (original != null)
                    {
                        if (original.Name != brand.Name) changes.Add($"Name: '{original.Name}' -> '{brand.Name}'");
                        if (original.Country != brand.Country) changes.Add($"Country: '{original.Country}' -> '{brand.Country}'");
                        if (original.Alias != brand.Alias) changes.Add($"Alias: '{original.Alias}' -> '{brand.Alias}'");
                        if (original.CEO != brand.CEO) changes.Add($"CEO: '{original.CEO}' -> '{brand.CEO}'");
                        if (original.Headquarters != brand.Headquarters) changes.Add("Headquarters updated");
                        if (original.Description != brand.Description) changes.Add("Description updated");
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

        // GET: ShopSouvenir/Brand/Delete/5
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

        // POST: ShopSouvenir/Brand/Delete/5
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
            }
            return RedirectToAction(nameof(Index));
        }

        private bool BrandExists(int id)
        {
            return _context.BrandSous.Any(e => e.Id == id);
        }
    }
}
