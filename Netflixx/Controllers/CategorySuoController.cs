using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Netflixx.Models;
using Netflixx.Models.Souvenir;
using Netflixx.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Netflixx.Controllers
{
    [Route("CategorySuo")]
    public class CategorySuoController : Controller
    {
        private readonly DBContext _context;

        public CategorySuoController(DBContext context)
        {
            _context = context;
        }

        // GET: /CategorySuo/
        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            try
            {
                var query = _context.CategorySous
                    .Include(c => c.Products)
                    .OrderByDescending(c => c.Id)
                    .AsQueryable();

                var totalItems = await query.CountAsync();
                var categories = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                ViewBag.CurrentPage = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalItems = totalItems;
                ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                return View("~/Views/CategorySouManeger/CategoryList.cshtml", categories);
            }
            catch (Exception ex)
            {
                // Log error here
                return StatusCode(500, "Đã xảy ra lỗi khi tải danh sách danh mục");
            }
        }

        // GET: /CategorySuo/Create
        [HttpGet("Create")]
        public IActionResult Create()
        {
            return View("~/Views/CategorySouManeger/CategoryCreate.cshtml");
        }

        // POST: /CategorySuo/Create
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] CategorySouModel category)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View("~/Views/CategorySouManeger/CategoryCreate.cshtml", category);
                }

                // Đảm bảo Products được khởi tạo
                if (category.Products == null)
                {
                    category.Products = new List<ProductSouModel>();
                }

                _context.CategorySous.Add(category);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Tạo danh mục thành công", redirect = Url.Action("Index") });
            }
            catch (Exception ex)
            {
                // Log error here
                return Json(new { success = false, message = "Có lỗi xảy ra khi tạo danh mục: " + ex.Message });
            }
        }

        // GET: /CategorySuo/Edit/{id}
        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(int id, int productPage = 1, int productPageSize = 9)
        {
            try
            {
                var category = await _context.CategorySous
                    .Include(c => c.Products)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (category == null)
                {
                    return NotFound();
                }

                // Pagination for products
                var productsQuery = category.Products.AsQueryable();
                var totalProducts = productsQuery.Count();
                var paginatedProducts = productsQuery
                    .Skip((productPage - 1) * productPageSize)
                    .Take(productPageSize)
                    .ToList();

                ViewBag.ProductPage = productPage;
                ViewBag.ProductPageSize = productPageSize;
                ViewBag.TotalProducts = totalProducts;
                ViewBag.TotalProductPages = (int)Math.Ceiling(totalProducts / (double)productPageSize);

                category.Products = paginatedProducts;

                return View("~/Views/CategorySouManeger/CategoryEdit.cshtml", category);
            }
            catch (Exception ex)
            {
                // Log error here
                return StatusCode(500, "Đã xảy ra lỗi khi tải trang chỉnh sửa");
            }
        }

        // POST: /CategorySuo/Update
        [HttpPost("Update")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update([FromBody] CategorySouModel category)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return Json(new
                {
                    success = false,
                    message = "Dữ liệu không hợp lệ",
                    errors = errors
                });
            }

            try
            {
                var existingCategory = await _context.CategorySous.FindAsync(category.Id);
                if (existingCategory == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Không tìm thấy danh mục"
                    });
                }

                // Kiểm tra trùng tên danh mục
                var duplicateName = await _context.CategorySous
                    .AnyAsync(c => c.Name == category.Name && c.Id != category.Id);

                if (duplicateName)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Tên danh mục đã tồn tại"
                    });
                }

                // Cập nhật thông tin
                existingCategory.Name = category.Name;
                existingCategory.Description = category.Description;

                _context.Update(existingCategory);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Cập nhật danh mục thành công",
                    data = new
                    {
                        id = existingCategory.Id,
                        name = existingCategory.Name,
                        description = existingCategory.Description
                    }
                });
            }
            catch (DbUpdateException ex)
            {
                // Log lỗi database
                return Json(new
                {
                    success = false,
                    message = "Lỗi khi lưu vào database",
                    error = ex.InnerException?.Message ?? ex.Message
                });
            }
            catch (Exception ex)
            {
                // Log lỗi hệ thống
                return Json(new
                {
                    success = false,
                    message = "Lỗi hệ thống khi cập nhật",
                    error = ex.Message
                });
            }
        }

        // POST: /CategorySuo/Delete/{id}
        [HttpPost("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var category = await _context.CategorySous
                    .Include(c => c.Products)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (category == null)
                {
                    return Json(new { success = false, message = "Danh mục không tồn tại." });
                }

                // Update products in this category
                foreach (var product in category.Products.ToList())
                {
                    product.CategoryId = null;
                    _context.Update(product);
                }

                _context.CategorySous.Remove(category);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Xóa danh mục thành công!" });
            }
            catch (Exception ex)
            {
                // Log error here
                return Json(new { success = false, message = "Lỗi khi xóa danh mục: " + ex.Message });
            }
        }

        // GET: /CategorySuo/SearchAvailableProducts
        [HttpGet("SearchAvailableProducts")]
        public async Task<IActionResult> SearchAvailableProducts(int categoryId, string searchTerm = "")
        {
            try
            {
                var productsInCategory = await _context.CategorySous
                    .Where(c => c.Id == categoryId)
                    .SelectMany(c => c.Products)
                    .Select(p => p.Id)
                    .ToListAsync();

                var query = _context.ProductSous
                    .Where(p => !productsInCategory.Contains(p.Id));

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    query = query.Where(p => p.Name.Contains(searchTerm));
                }

                var products = await query
                    .Select(p => new {
                        id = p.Id,
                        name = p.Name,
                        imageUrl = p.ImageUrl,
                        stockQuantity = p.StockQuantity
                    })
                    .ToListAsync();

                return Json(products);
            }
            catch (Exception ex)
            {
                // Log error here
                return StatusCode(500, new { error = "Đã xảy ra lỗi khi tìm kiếm sản phẩm" });
            }
        }

        // POST: /CategorySuo/AddProductsToCategory
        [HttpPost("AddProductsToCategory")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProductsToCategory([FromBody] AddProductsToCategoryRequestModel request)
        {
            try
            {
                var category = await _context.CategorySous
                    .Include(c => c.Products)
                    .FirstOrDefaultAsync(c => c.Id == request.CategoryId);

                if (category == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy danh mục" });
                }

                var productsToAdd = await _context.ProductSous
                    .Where(p => request.ProductIds.Contains(p.Id))
                    .ToListAsync();

                foreach (var product in productsToAdd)
                {
                    if (!category.Products.Any(p => p.Id == product.Id))
                    {
                        category.Products.Add(product);
                    }
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Thêm sản phẩm vào danh mục thành công" });
            }
            catch (Exception ex)
            {
                // Log error here
                return Json(new { success = false, message = "Đã xảy ra lỗi khi thêm sản phẩm vào danh mục: " + ex.Message });
            }
        }

        // POST: /CategorySuo/RemoveProduct
        [HttpPost("RemoveProduct")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveProduct([FromBody] RemoveProductFromCategoryRequestModel request)
        {
            try
            {
                var category = await _context.CategorySous
                    .Include(c => c.Products)
                    .FirstOrDefaultAsync(c => c.Id == request.CategoryId);

                if (category == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy danh mục" });
                }

                var productToRemove = category.Products.FirstOrDefault(p => p.Id == request.ProductId);
                if (productToRemove != null)
                {
                    category.Products.Remove(productToRemove);
                    await _context.SaveChangesAsync();

                    return Json(new
                    {
                        success = true,
                        message = "Xóa sản phẩm thành công",
                        productName = productToRemove.Name
                    });
                }

                return Json(new { success = false, message = "Không tìm thấy sản phẩm trong danh mục này" });
            }
            catch (Exception ex)
            {
                // Log error here
                return Json(new
                {
                    success = false,
                    message = "Lỗi khi xóa sản phẩm: " + ex.Message
                });
            }
        }
    }

    public class AddProductsToCategoryRequestModel
    {
        public int CategoryId { get; set; }
        public List<int> ProductIds { get; set; }
    }

    public class RemoveProductFromCategoryRequestModel
    {
        public int CategoryId { get; set; }
        public int ProductId { get; set; }
    }
}