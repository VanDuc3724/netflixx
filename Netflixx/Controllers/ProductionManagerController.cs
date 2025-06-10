using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Netflixx.Repositories;
using ProductionManagerApp.Models;

namespace Netflixx.Controllers
{
    public class ProductionManagerController : Controller
    {
        private readonly DBContext _context;

        public ProductionManagerController(DBContext context)
        {
            _context = context;
        }

        // GET: ProductionManager
        public async Task<IActionResult> Index(string searchString, string sortOrder, int? pageNumber)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["NameSortParm"] = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["CountrySortParm"] = sortOrder == "Country" ? "country_desc" : "Country";
            ViewData["YearSortParm"] = sortOrder == "Year" ? "year_desc" : "Year";

            var productionManagers = from pm in _context.ProductionManagers
                                     select pm;

            // Tìm kiếm
            if (!String.IsNullOrEmpty(searchString))
            {
                productionManagers = productionManagers.Where(pm => pm.Name.Contains(searchString)
                                                       || pm.Country.Contains(searchString)
                                                       || pm.CEO.Contains(searchString));
            }

            // Sắp xếp
            switch (sortOrder)
            {
                case "name_desc":
                    productionManagers = productionManagers.OrderByDescending(pm => pm.Name);
                    break;
                case "Country":
                    productionManagers = productionManagers.OrderBy(pm => pm.Country);
                    break;
                case "country_desc":
                    productionManagers = productionManagers.OrderByDescending(pm => pm.Country);
                    break;
                case "Year":
                    productionManagers = productionManagers.OrderBy(pm => pm.EstablishedYear);
                    break;
                case "year_desc":
                    productionManagers = productionManagers.OrderByDescending(pm => pm.EstablishedYear);
                    break;
                default:
                    productionManagers = productionManagers.OrderBy(pm => pm.Name);
                    break;
            }

            int pageSize = 10;
            return View(await PaginatedList<ProductionManager>.CreateAsync(productionManagers.AsNoTracking(), pageNumber ?? 1, pageSize));
        }

        // GET: ProductionManager/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var productionManager = await _context.ProductionManagers
                .Include(pm => pm.Films)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (productionManager == null)
            {
                return NotFound();
            }

            return View(productionManager);
        }

        // GET: ProductionManager/Create
        public IActionResult Create()
        {
            var model = new ProductionManager
            {
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            return View(model);
        }

        // POST: ProductionManager/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Website,Country,EstablishedYear,Alias,CEO,Headquarters,Description,LogoUrl")] ProductionManager productionManager)
        {
            if (ModelState.IsValid)
            {
                productionManager.CreatedAt = DateTime.Now;
                productionManager.UpdatedAt = DateTime.Now;

                _context.Add(productionManager);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Thêm công ty sản xuất thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(productionManager);
        }

        // GET: ProductionManager/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var productionManager = await _context.ProductionManagers.FindAsync(id);
            if (productionManager == null)
            {
                return NotFound();
            }
            return View(productionManager);
        }

        // POST: ProductionManager/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Website,Country,EstablishedYear,Alias,CEO,Headquarters,Description,LogoUrl,CreatedAt")] ProductionManager productionManager)
        {
            if (id != productionManager.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    productionManager.UpdatedAt = DateTime.Now;
                    _context.Update(productionManager);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Cập nhật công ty sản xuất thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductionManagerExists(productionManager.Id))
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
            return View(productionManager);
        }

        // GET: ProductionManager/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var productionManager = await _context.ProductionManagers
                .Include(pm => pm.Films)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (productionManager == null)
            {
                return NotFound();
            }

            return View(productionManager);
        }

        // POST: ProductionManager/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var productionManager = await _context.ProductionManagers
                .Include(pm => pm.Films)
                .FirstOrDefaultAsync(pm => pm.Id == id);

            if (productionManager != null)
            {
                if (productionManager.Films.Any())
                {
                    TempData["ErrorMessage"] = "Không thể xóa công ty sản xuất này vì còn có phim liên quan!";
                    return RedirectToAction(nameof(Index));
                }

                _context.ProductionManagers.Remove(productionManager);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xóa công ty sản xuất thành công!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ProductionManagerExists(int id)
        {
            return _context.ProductionManagers.Any(e => e.Id == id);
        }
    }
}