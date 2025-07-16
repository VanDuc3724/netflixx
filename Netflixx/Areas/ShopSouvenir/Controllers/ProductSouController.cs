using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Netflixx.Models;
using Netflixx.Models.Souvenir;
using Netflixx.Models.ViewModel;
using Netflixx.Repositories;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static System.Net.WebRequestMethods;

namespace Netflixx.Areas.ShopSouvenir.Controllers
{
    [Area("ShopSouvenir")]
    public class ProductSouController : Controller
    {
        private readonly DBContext _context;
        private readonly IWebHostEnvironment _webHostEn;

        public ProductSouController(DBContext context, IWebHostEnvironment webHostEn)
        {
            _context = context;
            _webHostEn = webHostEn;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var products = await _context.ProductSous
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Series)
                .ToListAsync();
            return View(products);
        }

        [HttpGet]
        public IActionResult Create()
        {
            // Lấy dữ liệu và log để debug
            var categories = _context.CategorySous.ToList();
            var brands = _context.BrandSous.ToList();
            var series = _context.SeriesSous.ToList();

            Console.WriteLine($"First category: {categories.FirstOrDefault()?.Name}");

            // Tạo SelectList với kiểm tra null
            ViewBag.CategoryId = categories.Any()
                ? new SelectList(categories, "Id", "Name")
                : new SelectList(Enumerable.Empty<SelectListItem>());

            ViewBag.BrandId = brands.Any()
                ? new SelectList(brands, "Id", "Name")
                : new SelectList(Enumerable.Empty<SelectListItem>());

            ViewBag.SeriesId = series.Any()
                ? new SelectList(series, "Id", "Name")
                : new SelectList(Enumerable.Empty<SelectListItem>());

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
    [Bind("Name,CategoryId,BrandId,SeriesId,Price,StockQuantity,Description,ImageUpload")]
    ProductSouModel product)
        {
            Console.WriteLine($"Received model: Name={product.Name}, CategoryId={product.CategoryId}, BrandId={product.BrandId}, SeriesId={product.SeriesId}");
            // Debug received values
            Console.WriteLine($"Received values - Price: {product.Price}, Stock: {product.StockQuantity}");

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, Errors = x.Value.Errors.Select(e => e.ErrorMessage) });

                Console.WriteLine("Validation errors:");
                foreach (var error in errors)
                {
                    Console.WriteLine($"{error.Key}: {string.Join(", ", error.Errors)}");
                }

                // Repopulate dropdowns
                ViewBag.CategoryId = new SelectList(_context.CategorySous, "Id", "Name", product.CategoryId);
                ViewBag.BrandId = new SelectList(_context.BrandSous, "Id", "Name", product.BrandId);
                ViewBag.SeriesId = new SelectList(_context.SeriesSous, "Id", "Name", product.SeriesId);

                return View(product);
            }
            foreach (var entry in ModelState)
            {
                Console.WriteLine($"{entry.Key}: {string.Join(", ", entry.Value.Errors.Select(e => e.ErrorMessage))}");
            }

            if (!ModelState.IsValid)
            {
                // Load lại dữ liệu dropdown
                ViewBag.CategoryId = new SelectList(_context.CategorySous, "Id", "Name", product.CategoryId);
                ViewBag.BrandId = new SelectList(_context.BrandSous, "Id", "Name", product.BrandId);
                ViewBag.SeriesId = new SelectList(_context.SeriesSous, "Id", "Name", product.SeriesId);

                return View(product); // Trả về view với model hiện tại
            }

            try
            {
                // Xử lý upload file
                if (product.ImageUpload != null && product.ImageUpload.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_webHostEn.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + product.ImageUpload.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await product.ImageUpload.CopyToAsync(fileStream);
                    }

                    product.ImageUrl = "/uploads/" + uniqueFileName;
                }

                // Debug trước khi save
                Console.WriteLine($"Saving product: {System.Text.Json.JsonSerializer.Serialize(product)}");

                _context.ProductSous.Add(product);
                await _context.SaveChangesAsync(); // Đảm bảo có await

                TempData["success"] = "Thêm sản phẩm thành công";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Log lỗi chi tiết
                Console.WriteLine($"Error saving product: {ex}");

                TempData["error"] = "Lỗi khi lưu sản phẩm: ";

                // Load lại dữ liệu dropdown
                ViewBag.CategoryId = new SelectList(_context.CategorySous, "Id", "Name", product.CategoryId);
                ViewBag.BrandId = new SelectList(_context.BrandSous, "Id", "Name", product.BrandId);
                ViewBag.SeriesId = new SelectList(_context.SeriesSous, "Id", "Name", product.SeriesId);

                return View(product);
            }
        }

        

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,CategoryId,BrandId,SeriesId,Price,StockQuantity,Description,ImageUpload,ImageUrl")] ProductSouModel product)
        {
            Console.WriteLine($"Received model for edit: Id={id}, Name={product.Name}, CategoryId={product.CategoryId}, BrandId={product.BrandId}, SeriesId={product.SeriesId}");
            Console.WriteLine($"Received values - Price: {product.Price}, Stock: {product.StockQuantity}");

            if (id != product.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, Errors = x.Value.Errors.Select(e => e.ErrorMessage) });
                Console.WriteLine("Validation errors:");
                foreach (var error in errors)
                {
                    Console.WriteLine($"{error.Key}: {string.Join(", ", error.Errors)}");
                }

                // Repopulate dropdowns with selected values
                ViewBag.CategoryId = new SelectList(_context.CategorySous, "Id", "Name", product.CategoryId);
                ViewBag.BrandId = new SelectList(_context.BrandSous, "Id", "Name", product.BrandId);
                ViewBag.SeriesId = new SelectList(_context.SeriesSous, "Id", "Name", product.SeriesId);

                return View(product);
            }

            try
            {
                var existingProduct = await _context.ProductSous.FindAsync(id);
                if (existingProduct == null)
                {
                    return NotFound();
                }

                // Update existing product properties
                existingProduct.Name = product.Name;
                existingProduct.CategoryId = product.CategoryId;
                existingProduct.BrandId = product.BrandId;
                existingProduct.SeriesId = product.SeriesId;
                existingProduct.Price = product.Price;
                existingProduct.StockQuantity = product.StockQuantity;
                existingProduct.Description = product.Description;

                // Handle image upload if a new file is provided
                if (product.ImageUpload != null && product.ImageUpload.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_webHostEn.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    // Delete old image if it exists
                    if (!string.IsNullOrEmpty(existingProduct.ImageUrl) && System.IO.File.Exists(Path.Combine(_webHostEn.WebRootPath, existingProduct.ImageUrl.TrimStart('/'))))
                    {
                        System.IO.File.Delete(Path.Combine(_webHostEn.WebRootPath, existingProduct.ImageUrl.TrimStart('/')));
                    }

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + product.ImageUpload.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await product.ImageUpload.CopyToAsync(fileStream);
                    }

                    existingProduct.ImageUrl = "/uploads/" + uniqueFileName;
                }

                // Debug before save
                Console.WriteLine($"Updating product: {System.Text.Json.JsonSerializer.Serialize(existingProduct)}");

                _context.Entry(existingProduct).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                TempData["success"] = "Cập nhật sản phẩm thành công";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating product: {ex}");

                TempData["error"] = "Lỗi khi cập nhật sản phẩm: " + ex.Message;

                // Repopulate dropdowns with selected values
                ViewBag.CategoryId = new SelectList(_context.CategorySous, "Id", "Name", product.CategoryId);
                ViewBag.BrandId = new SelectList(_context.BrandSous, "Id", "Name", product.BrandId);
                ViewBag.SeriesId = new SelectList(_context.SeriesSous, "Id", "Name", product.SeriesId);

                return View(product);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.ProductSous
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Series)
                .FirstOrDefaultAsync(p => p.Id == id);

                // Load lại dữ liệu dropdown
                ViewBag.CategoryId = new SelectList(_context.CategorySous, "Id", "Name", product.CategoryId);
                ViewBag.BrandId = new SelectList(_context.BrandSous, "Id", "Name", product.BrandId);
                ViewBag.SeriesId = new SelectList(_context.SeriesSous, "Id", "Name", product.SeriesId);

            return View(product);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.ProductSous.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            try
            {
                // Removed image deletion logic
                _context.ProductSous.Remove(product);
                await _context.SaveChangesAsync();

                TempData["success"] = "Xóa sản phẩm thành công";
                return Redirect("https://localhost:7198/ShopSouvenir/ProductSou/Index");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting product: {ex}");
                TempData["error"] = "Lỗi khi xóa sản phẩm: " + ex.Message;
                return Redirect("https://localhost:7198/ShopSouvenir/ProductSou/Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> DetailsPartial(int id)
        {
            var product = await _context.ProductSous
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Series)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            return PartialView("_ProductDetailsPartial", product);
        }
    }
}

