using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductionManagerApp.Models;


namespace ProductionManagerApp.Controllers
{
    public class ProductionManagerController : Controller
    {
        private readonly DbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductionManagerController(DbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: ProductionManager
        public async Task<IActionResult> Index(string searchString, string sortOrder, int? pageNumber)
        {
            ViewData["CurrentFilter"] = searchString;
            ViewData["NameSortParm"] = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["CountrySortParm"] = sortOrder == "Country" ? "country_desc" : "Country";
            ViewData["DateSortParm"] = sortOrder == "Date" ? "date_desc" : "Date";

            var productionManagers = from pm in _context.ProductionManager
                                     select pm;

            if (!String.IsNullOrEmpty(searchString))
            {
                productionManagers = productionManagers.Where(s => s.Name.Contains(searchString)
                                       || s.Country.Contains(searchString)
                                       || s.CEO.Contains(searchString));
            }

            switch (sortOrder)
            {
                case "name_desc":
                    productionManagers = productionManagers.OrderByDescending(s => s.Name);
                    break;
                case "Country":
                    productionManagers = productionManagers.OrderBy(s => s.Country);
                    break;
                case "country_desc":
                    productionManagers = productionManagers.OrderByDescending(s => s.Country);
                    break;
                case "Date":
                    productionManagers = productionManagers.OrderBy(s => s.EstablishedDate);
                    break;
                case "date_desc":
                    productionManagers = productionManagers.OrderByDescending(s => s.EstablishedDate);
                    break;
                default:
                    productionManagers = productionManagers.OrderBy(s => s.Name);
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
                .Include(p => p.Films)
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
            return View();
        }

        // POST: ProductionManager/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Website,Country,EstablishedDate,Alias,CEO,Headquarters,Description,LogoFile")] ProductionManager productionManager)
        {
            if (ModelState.IsValid)
            {
                // Handle logo upload
                if (productionManager.LogoFile != null)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "logos");
                    Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + productionManager.LogoFile.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await productionManager.LogoFile.CopyToAsync(fileStream);
                    }

                    productionManager.LogoUrl = "/images/logos/" + uniqueFileName;
                }

                productionManager.CreatedAt = DateTime.Now;
                productionManager.UpdatedAt = DateTime.Now;

                _context.Add(productionManager);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Công ty sản xuất đã được tạo thành công!";
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
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Website,Country,EstablishedDate,Alias,CEO,Headquarters,Description,LogoUrl,LogoFile,CreatedAt")] ProductionManager productionManager)
        {
            if (id != productionManager.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Handle logo upload
                    if (productionManager.LogoFile != null)
                    {
                        // Delete old logo if exists
                        if (!string.IsNullOrEmpty(productionManager.LogoUrl))
                        {
                            string oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, productionManager.LogoUrl.TrimStart('/'));
                            if (System.IO.File.Exists(oldFilePath))
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                        }

                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "logos");
                        Directory.CreateDirectory(uploadsFolder);

                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + productionManager.LogoFile.FileName;
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await productionManager.LogoFile.CopyToAsync(fileStream);
                        }

                        productionManager.LogoUrl = "/images/logos/" + uniqueFileName;
                    }

                    productionManager.UpdatedAt = DateTime.Now;
                    _context.Update(productionManager);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Công ty sản xuất đã được cập nhật thành công!";
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
            var productionManager = await _context.ProductionManagers.FindAsync(id);

            if (productionManager != null)
            {
                // Delete logo file if exists
                if (!string.IsNullOrEmpty(productionManager.LogoUrl))
                {
                    string filePath = Path.Combine(_webHostEnvironment.WebRootPath, productionManager.LogoUrl.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                _context.ProductionManagers.Remove(productionManager);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Công ty sản xuất đã được xóa thành công!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ProductionManagerExists(int id)
        {
            return _context.ProductionManagers.Any(e => e.Id == id);
        }
    }

    // Helper class for pagination
    public class PaginatedList<T> : List<T>
    {
        public int PageIndex { get; private set; }
        public int TotalPages { get; private set; }

        public PaginatedList(List<T> items, int count, int pageIndex, int pageSize)
        {
            PageIndex = pageIndex;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);

            this.AddRange(items);
        }

        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;

        public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> source, int pageIndex, int pageSize)
        {
            var count = await source.CountAsync();
            var items = await source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PaginatedList<T>(items, count, pageIndex, pageSize);
        }
    }
}