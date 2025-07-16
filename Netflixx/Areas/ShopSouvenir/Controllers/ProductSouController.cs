using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Netflixx.Models;
using Netflixx.Models.Souvenir;
using Netflixx.Models.ViewModel;
using Netflixx.Repositories;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;

namespace Netflixx.Areas.ShopSouvenir.Controllers
{
    [Area("ShopSouvenir")]
    public class ProductSouController : Controller
    {
        private readonly DBContext _context;

        public ProductSouController(DBContext context)
        {
            _context = context;
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

        public IActionResult Create()
        {
            // Đảm bảo dữ liệu được lấy đúng
            var categories = _context.CategorySous
                .Select(c => new { c.Id, c.Name })
                .ToList();

            var viewModel = new ProductCreateViewModel
            {
                Product = new ProductSouModel(),
                Categories = new SelectList(categories, "Id", "Name"),
                Brands = new SelectList(_context.BrandSous.ToList(), "Id", "Name"),
                Series = new SelectList(_context.SeriesSous.ToList(), "Id", "Name")
            };

            // Debug cuối cùng
            Console.WriteLine($"Categories count: {viewModel.Categories.Count()}");
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Product,Categories,Brands,Series")] ProductCreateViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                // Handle file upload
                if (viewModel.Product.ImageUpload != null && viewModel.Product.ImageUpload.Length > 0)
                {
                    var uploadsFolder = Path.Combine("wwwroot", "image", "products");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + viewModel.Product.ImageUpload.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await viewModel.Product.ImageUpload.CopyToAsync(fileStream);
                    }

                    viewModel.Product.ImageUrl = "/image/products/" + uniqueFileName;
                }

                _context.Add(viewModel.Product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Repopulate dropdowns if validation fails
            viewModel.Categories = new SelectList(_context.CategorySous.ToList(), "Id", "Name");
            viewModel.Brands = new SelectList(_context.BrandSous.ToList(), "Id", "Name");
            viewModel.Series = new SelectList(_context.SeriesSous.ToList(), "Id", "Name");

            return View(viewModel);
        }

        // GET: ShopSouvenir/Manager/ProductSou/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.ProductSous.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            ViewBag.Categories = _context.CategorySous.ToList();
            ViewBag.Brands = _context.BrandSous.ToList();
            ViewBag.Series = _context.SeriesSous.ToList();
            return View(product);
        }

        // POST: ShopSouvenir/Manager/ProductSou/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductSouModel product)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(product);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id))
                    {
                        return NotFound();
                    }
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = _context.CategorySous.ToList();
            ViewBag.Brands = _context.BrandSous.ToList();
            ViewBag.Series = _context.SeriesSous.ToList();
            return View(product);
        }

        // GET: ShopSouvenir/Manager/ProductSou/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.ProductSous
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Series)
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // GET: ShopSouvenir/Manager/ProductSou/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.ProductSous
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Series)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: ShopSouvenir/Manager/ProductSou/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.ProductSous.FindAsync(id);
            if (product != null)
            {
                _context.ProductSous.Remove(product);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            return _context.ProductSous.Any(e => e.Id == id);
        }
    }
}