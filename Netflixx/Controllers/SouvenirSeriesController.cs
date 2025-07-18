using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Netflixx.Models;
using Netflixx.Models.Souvenir;
using Netflixx.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Netflixx.Controllers
{
    public class SouvenirSeriesController : Controller
    {
        private readonly DBContext _context;

        public SouvenirSeriesController(DBContext context)
        {
            _context = context;
        }

        // GET: /SouvenirSeries/
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            var query = _context.SeriesSous
                .Include(s => s.Products)
                .OrderByDescending(s => s.Id)
                .AsQueryable();

            var totalItems = await query.CountAsync();
            var seriesList = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            return View("~/Views/Series/SeriesList.cshtml", seriesList);
        }

        // GET: /SouvenirSeries/Detail/{id}
        public async Task<IActionResult> Detail(int id, int productPage = 1, int productPageSize = 9)
        {
            var series = await _context.SeriesSous
                .Include(s => s.Products)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (series == null)
            {
                return NotFound();
            }

            // Áp dụng phân trang cho sản phẩm
            var productsQuery = series.Products.AsQueryable();
            var totalProducts = productsQuery.Count();
            var paginatedProducts = productsQuery
                .Skip((productPage - 1) * productPageSize)
                .Take(productPageSize)
                .ToList();

            ViewBag.ProductPage = productPage;
            ViewBag.ProductPageSize = productPageSize;
            ViewBag.TotalProducts = totalProducts;
            ViewBag.TotalProductPages = (int)Math.Ceiling(totalProducts / (double)productPageSize);

            // Thay thế danh sách sản phẩm bằng phiên bản đã phân trang
            series.Products = paginatedProducts;

            return View("~/Views/Series/SeriesDetail.cshtml", series);
        }

        // GET: /SouvenirSeries/Create
        public IActionResult Create()
        {
            return View("~/Views/Series/SeriesCreate.cshtml", new SeriesSouModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SeriesSouModel series)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(series);
                    await _context.SaveChangesAsync();

                    // Thêm thông báo thành công vào TempData
                    TempData["SuccessMessage"] = "Series đã được tạo thành công!";

                    // Trả về JSON thay vì Redirect để có thể xử lý thông báo trước
                    return Json(new { success = true, redirectUrl = Url.Action("Index") });
                }
                catch (Exception ex)
                {
                    // Trả về lỗi dưới dạng JSON
                    return Json(new { success = false, message = "Có lỗi xảy ra khi tạo series: " + ex.Message });
                }
            }

            // Nếu ModelState không hợp lệ, trả về lỗi validation
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return Json(new { success = false, message = "Dữ liệu không hợp lệ", errors });
        }

        // GET: /SouvenirSeries/Edit/{id}
        public async Task<IActionResult> Edit(int id, int productPage = 1, int productPageSize = 9)
        {
            var series = await _context.SeriesSous
                .Include(s => s.Products)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (series == null)
            {
                return NotFound();
            }

            // Áp dụng phân trang cho sản phẩm
            var productsQuery = series.Products.AsQueryable();
            var totalProducts = productsQuery.Count();
            var paginatedProducts = productsQuery
                .Skip((productPage - 1) * productPageSize)
                .Take(productPageSize)
                .ToList();

            ViewBag.ProductPage = productPage;
            ViewBag.ProductPageSize = productPageSize;
            ViewBag.TotalProducts = totalProducts;
            ViewBag.TotalProductPages = (int)Math.Ceiling(totalProducts / (double)productPageSize);

            // Thay thế danh sách sản phẩm bằng phiên bản đã phân trang
            series.Products = paginatedProducts;

            return View("~/Views/Series/SeriesEdit.cshtml", series);
        }

        // POST: /SouvenirSeries/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromBody] SeriesSouModel series)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var existingSeries = await _context.SeriesSous.FindAsync(series.Id);
                    if (existingSeries == null)
                    {
                        return Json(new { success = false, message = "Không tìm thấy series" });
                    }

                    // Cập nhật chỉ các trường cần thiết
                    existingSeries.Name = series.Name;
                    existingSeries.Description = series.Description;

                    _context.Update(existingSeries);
                    await _context.SaveChangesAsync();

                    return Json(new
                    {
                        success = true,
                        message = "Cập nhật series thành công!",
                        redirectUrl = Url.Action("Detail", new { id = series.Id })
                    });
                }
                catch (Exception ex)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Có lỗi xảy ra khi cập nhật series: " + ex.Message
                    });
                }
            }

            // Nếu ModelState không hợp lệ
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return Json(new
            {
                success = false,
                message = "Dữ liệu không hợp lệ",
                errors
            });
        }

        // GET: /SouvenirSeries/SearchAvailableProducts
        [HttpGet]
        public async Task<IActionResult> SearchAvailableProducts(int seriesId, string searchTerm = "")
        {
            // Lấy các sản phẩm chưa thuộc series này
            var productsInSeries = await _context.SeriesSous
                .Where(s => s.Id == seriesId)
                .SelectMany(s => s.Products)
                .Select(p => p.Id)
                .ToListAsync();

            var query = _context.ProductSous
                .Where(p => !productsInSeries.Contains(p.Id));

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(p => p.Name.Contains(searchTerm));
            }

            var products = await query
                .Select(p => new { id = p.Id, name = p.Name })
                .ToListAsync();

            return Json(products);
        }

        // POST: /SouvenirSeries/AddProductsToSeries
        [HttpPost]
        public async Task<IActionResult> AddProductsToSeries([FromBody] AddProductsToSeriesRequest request)
        {
            var series = await _context.SeriesSous
                .Include(s => s.Products)
                .FirstOrDefaultAsync(s => s.Id == request.SeriesId);

            if (series == null)
            {
                return NotFound();
            }

            var productsToAdd = await _context.ProductSous
                .Where(p => request.ProductIds.Contains(p.Id))
                .ToListAsync();

            foreach (var product in productsToAdd)
            {
                if (!series.Products.Any(p => p.Id == product.Id))
                {
                    series.Products.Add(product);
                }
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        // POST: /SouvenirSeries/RemoveProduct
        [HttpPost]
        public async Task<IActionResult> RemoveProduct([FromBody] RemoveProductRequest request)
        {
            try
            {
                var series = await _context.SeriesSous
                    .Include(s => s.Products)
                    .FirstOrDefaultAsync(s => s.Id == request.SeriesId);

                if (series == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy bộ sưu tập" });
                }

                var productToRemove = series.Products.FirstOrDefault(p => p.Id == request.ProductId);
                if (productToRemove != null)
                {
                    series.Products.Remove(productToRemove);
                    await _context.SaveChangesAsync();

                    return Json(new
                    {
                        success = true,
                        message = "Xóa sản phẩm thành công",
                        productName = productToRemove.Name
                    });
                }

                return Json(new { success = false, message = "Không tìm thấy sản phẩm trong bộ sưu tập này" });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Lỗi khi xóa sản phẩm: " + ex.Message
                });
            }
        }

        // POST: /SouvenirSeries/Delete/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var series = await _context.SeriesSous
                    .Include(s => s.Products)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (series == null)
                {
                    return Json(new { success = false, message = "Series không tồn tại." });
                }

                // Cập nhật sản phẩm thuộc Series
                foreach (var product in series.Products.ToList())
                {
                    product.SeriesId = null;
                    _context.Update(product);
                }

                _context.SeriesSous.Remove(series);
                await _context.SaveChangesAsync();

                // Luôn trả về JSON
                return Json(new
                {
                    success = true,
                    message = "Xóa Series thành công!"
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Lỗi khi xóa Series: " + ex.Message
                });
            }
        }
    }



    public class AddProductsToSeriesRequest
    {
        public int SeriesId { get; set; }
        public List<int> ProductIds { get; set; }
    }

    public class RemoveProductRequest
    {
        public int SeriesId { get; set; }
        public int ProductId { get; set; }
    }
}