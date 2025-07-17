using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Hosting;
using Netflixx.Models;
using Netflixx.Repositories;
using ProductionManagerApp.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Netflixx.Controllers
{
    [Authorize]
    public class FilmController1 : Controller
    {
        private readonly DBContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;
        private const string UploadsFolder = "uploads";
        private const string SystemUser = "System";

        public FilmController1(DBContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        // Danh sách phim chính
        public async Task<IActionResult> Index(string searchTerm, string genre, string status, string rating, int? year, string duration, string price)
        {
            // Lấy dữ liệu filter từ database
            var filterData = new FilmFilterViewModel
            {
                Genres = await _context.Films
                    .Where(f => !string.IsNullOrEmpty(f.Genre) && f.Status != "Deleted")
                    .Select(f => f.Genre)
                    .Distinct()
                    .OrderBy(g => g)
                    .ToListAsync(),

                Statuses = await _context.Films
                    .Where(f => !string.IsNullOrEmpty(f.Status) && f.Status != "Deleted")
                    .Select(f => f.Status)
                    .Distinct()
                    .OrderBy(s => s)
                    .ToListAsync(),

                Ratings = await _context.Films
                    .Where(f => !string.IsNullOrEmpty(f.RatingValue) && f.Status != "Deleted")
                    .Select(f => f.RatingValue)
                    .Distinct()
                    .OrderBy(r => r)
                    .ToListAsync(),

                ReleaseYears = await _context.Films
                    .Where(f => f.ReleaseDate.HasValue && f.Status != "Deleted")
                    .Select(f => f.ReleaseDate.Value.Year)
                    .Distinct()
                    .OrderByDescending(y => y)
                    .Select(y => y.ToString())
                    .ToListAsync()
            };

            ViewBag.FilterData = filterData;

            // Xử lý lọc dữ liệu
            var query = _context.Films
                .Where(f => f.Status != "Deleted")
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(f => f.Title.Contains(searchTerm) ||
                                       f.Description.Contains(searchTerm) ||
                                       f.Actors.Contains(searchTerm));
            }

            if (!string.IsNullOrEmpty(genre))
            {
                query = query.Where(f => f.Genre == genre);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(f => f.Status == status);
            }

            if (!string.IsNullOrEmpty(rating))
            {
                query = query.Where(f => f.RatingValue == rating);
            }

            if (year.HasValue)
            {
                query = query.Where(f => f.ReleaseDate.HasValue &&
                                       f.ReleaseDate.Value.Year == year.Value);
            }

            // Lọc theo khoảng thời lượng
            if (!string.IsNullOrEmpty(duration))
            {
                var range = duration.Split('-');
                if (range.Length == 2)
                {
                    var min = int.Parse(range[0]);
                    var max = int.Parse(range[1]);
                    query = query.Where(f => f.Duration >= min && f.Duration <= max);
                }
                else if (duration == "120")
                {
                    query = query.Where(f => f.Duration > 120);
                }
            }

            // Lọc theo khoảng giá
            if (!string.IsNullOrEmpty(price))
            {
                if (price == "0")
                {
                    query = query.Where(f => f.Price == 0 || f.Price == null);
                }
                else
                {
                    var range = price.Split('-');
                    if (range.Length == 2)
                    {
                        var min = decimal.Parse(range[0]);
                        var max = decimal.Parse(range[1]);
                        query = query.Where(f => f.Price >= min && f.Price <= max);
                    }
                    else if (price == "100000")
                    {
                        query = query.Where(f => f.Price > 100000);
                    }
                }
            }

            var films = await query
                .OrderByDescending(f => f.ReleaseDate)
                .AsNoTracking()
                .ToListAsync();

            return View("~/Views/Film/ManagerFilm/ListFilms.cshtml", films);
        }

        public class FilmFilterViewModel
        {
            public List<string> Genres { get; set; }
            public List<string> Statuses { get; set; }
            public List<string> Ratings { get; set; }
            public List<string> ReleaseYears { get; set; }
        }


        // Tìm kiếm phim
        public async Task<IActionResult> Search(string searchTerm, string genre, int? year)
        {
            ViewBag.AvailableGenres = await GetUniqueGenresFromDatabase();
            ViewBag.AvailableYears = await GetUniqueReleaseYearsFromDatabase();

            var query = _context.Films
                .Where(f => f.Status != "Deleted")
                .AsQueryable();

            ViewBag.ProductionManagers = await _context.ProductionManagers.OrderBy(p => p.Name).ToListAsync();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(f => f.Title.Contains(searchTerm) ||
                                       f.Description.Contains(searchTerm) ||
                                       f.Actors.Contains(searchTerm));
            }

            if (!string.IsNullOrEmpty(genre))
            {
                query = query.Where(f => f.Genre == genre);
            }

            if (year.HasValue)
            {
                query = query.Where(f => f.ReleaseDate.HasValue &&
                                       f.ReleaseDate.Value.Year == year.Value);
            }

            var result = await query
                .OrderByDescending(f => f.ReleaseDate)
                .AsNoTracking()
                .ToListAsync();

            return View("~/Views/Film/ManagerFilm/SearchFilms.cshtml", result);
        }

        // Thêm phim mới
        [HttpGet]
        public IActionResult Create()
        {
            // Thêm danh sách quản lý sản xuất vào ViewBag để hiển thị dropdown
            ViewBag.ProductionManagers = _context.ProductionManagers
                .OrderBy(p => p.Name)
                .ToList();

            SetupCreateEditViewBags();
            return View("~/Views/Film/ManagerFilm/CreateFilms.cshtml", new FilmsModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FilmsModel film, IFormFile posterFile)
        {
            try
            {
                // 1. Kiểm tra validation cơ bản
                if (string.IsNullOrEmpty(film.Title))
                {
                    ModelState.AddModelError("Title", "Vui lòng nhập tên phim");
                }

                if (string.IsNullOrEmpty(film.Genre))
                {
                    ModelState.AddModelError("Genre", "Vui lòng chọn thể loại");
                }

                if (film.ReleaseDate == null)
                {
                    ModelState.AddModelError("ReleaseDate", "Vui lòng chọn ngày phát hành");
                }

                // 2. Kiểm tra ProductionManagerId (bắt buộc)
                if (!film.ProductionManagerId.HasValue || film.ProductionManagerId <= 0)
                {
                    ModelState.AddModelError("ProductionManagerId", "Vui lòng chọn quản lý sản xuất");
                }

                // 3. Kiểm tra poster (URL hoặc file upload)
                if (string.IsNullOrEmpty(film.PosterPath) && posterFile == null)
                {
                    ModelState.AddModelError("PosterPath", "Vui lòng cung cấp URL poster hoặc tải lên file poster");
                }

                // 4. Nếu có lỗi validation
                if (!ModelState.IsValid)
                {
                    // Load lại danh sách Production Managers cho dropdown
                    ViewBag.ProductionManagers = await _context.ProductionManagers
                        .OrderBy(p => p.Name)
                        .ToListAsync();

                    SetupCreateEditViewBags();
                    return View("~/Views/Film/ManagerFilm/CreateFilms.cshtml", film);
                }

                // 5. Xử lý upload file poster nếu có
                if (posterFile != null && posterFile.Length > 0)
                {
                    // Kiểm tra kích thước file (tối đa 5MB)
                    if (posterFile.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError("PosterPath", "Kích thước file không được vượt quá 5MB");
                        ViewBag.ProductionManagers = await _context.ProductionManagers.ToListAsync();
                        SetupCreateEditViewBags();
                        return View("~/Views/Film/ManagerFilm/CreateFilms.cshtml", film);
                    }

                    // Kiểm tra loại file
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var fileExtension = Path.GetExtension(posterFile.FileName).ToLower();
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        ModelState.AddModelError("PosterPath", "Chỉ chấp nhận file ảnh (JPG, JPEG, PNG, GIF)");
                        ViewBag.ProductionManagers = await _context.ProductionManagers.ToListAsync();
                        SetupCreateEditViewBags();
                        return View("~/Views/Film/ManagerFilm/CreateFilms.cshtml", film);
                    }

                    // Lưu file
                    var uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "uploads", "posters");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + posterFile.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await posterFile.CopyToAsync(fileStream);
                    }
                    film.PosterPath = "/uploads/posters/" + uniqueFileName;
                }

                // 6. Thiết lập thông tin tracking
                film.LastAction = "Create";
                film.ModifiedBy = User.Identity?.Name ?? SystemUser;
                film.ModificationDate = DateTime.Now;
                film.ChangeNote = "Thêm phim mới";
                film.Rating = 0; // Đảm bảo Rating có giá trị mặc định

                // 7. Thêm vào database
                _context.Films.Add(film);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Thêm phim thành công!";

                // 8. Redirect để tránh duplicate form submission
                return RedirectToAction("Create");
            }
            catch (Exception ex)
            {
                // Xử lý lỗi
                ModelState.AddModelError("", $"Lỗi khi thêm phim: {ex.Message}");

                // Load lại danh sách Production Managers khi có lỗi
                ViewBag.ProductionManagers = await _context.ProductionManagers
                    .OrderBy(p => p.Name)
                    .ToListAsync();

                SetupCreateEditViewBags();
                return View("~/Views/Film/ManagerFilm/CreateFilms.cshtml", film);
            }
        }


        // Sửa phim
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(FilmsModel updatedFilm)
        {
            // Bỏ qua validation cho các trường tracking và navigation property
            ModelState.Remove("LastAction");
            ModelState.Remove("ModifiedBy");
            ModelState.Remove("ModificationDate");
            ModelState.Remove("ModifiedFields");
            ModelState.Remove("ChangeNote");
            ModelState.Remove("ProductionManager"); // Navigation property
            ModelState.Remove("PromotionFilms");
            ModelState.Remove("Purchases");
            ModelState.Remove("FavoriteFilms");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );
                return Json(new { success = false, message = "Dữ liệu không hợp lệ", errors = errors });
            }

            try
            {
                var film = await _context.Films.FindAsync(updatedFilm.Id);

                if (film == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy phim" });
                }

                // Cập nhật từng property thủ công, trừ navigation properties
                film.Title = updatedFilm.Title;
                film.Genre = updatedFilm.Genre;
                film.Duration = updatedFilm.Duration;
                film.ReleaseDate = updatedFilm.ReleaseDate;
                film.Status = updatedFilm.Status;
                film.RatingValue = updatedFilm.RatingValue;
                film.Director = updatedFilm.Director;
                film.PosterPath = updatedFilm.PosterPath;
                film.Actors = updatedFilm.Actors;
                film.Description = updatedFilm.Description;
                film.TrailerURL = updatedFilm.TrailerURL;
                film.Price = updatedFilm.Price;
                film.FilmURL = updatedFilm.FilmURL;

                // Chỉ cập nhật ProductionManagerId nếu có giá trị hợp lệ
                if (updatedFilm.ProductionManagerId.HasValue && updatedFilm.ProductionManagerId > 0)
                {
                    film.ProductionManagerId = updatedFilm.ProductionManagerId;
                }

                // Thiết lập thông tin tracking
                film.LastAction = "Update";
                film.ModifiedBy = User.Identity?.Name ?? SystemUser;
                film.ModificationDate = DateTime.Now;
                film.ChangeNote = "Cập nhật thông tin phim";
                film.ModifiedFields = GetModifiedFields(film, updatedFilm);

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Cập nhật thành công!",
                    film = new
                    {
                        id = film.Id,
                        title = film.Title,
                        genre = film.Genre,
                        price = film.Price?.ToString("N0") ?? "0",
                        releaseDate = film.ReleaseDate?.ToString("dd/MM/yyyy") ?? "Chưa có thông tin",
                        status = film.GetStatusText(),
                        ratingValue = film.GetRatingText(),
                        rating = film.Rating,
                        ProductionManagerId = film.ProductionManagerId
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        // Phương thức helper mới để xác định các trường đã thay đổi
        private string GetModifiedFields(FilmsModel original, FilmsModel updated)
        {
            var changedFields = new List<string>();
            var properties = typeof(FilmsModel).GetProperties();

            foreach (var prop in properties)
            {
                if (ShouldTrackProperty(prop.Name))
                {
                    var oldValue = prop.GetValue(original);
                    var newValue = prop.GetValue(updated);

                    if (!Equals(oldValue, newValue))
                    {
                        var displayAttr = prop.GetCustomAttribute<DisplayAttribute>();
                        changedFields.Add(displayAttr?.Name ?? prop.Name);
                    }
                }
            }

            return changedFields.Any() ? string.Join(", ", changedFields) : null;
        }

        // Thùng rác - Danh sách phim đã xóa
        public async Task<IActionResult> Trash()
        {
            var deletedFilms = await _context.Films
                .Where(f => f.Status == "Deleted")
                .OrderByDescending(f => f.ModificationDate)
                .AsNoTracking()
                .ToListAsync();

            return View("~/Views/Film/ManagerFilm/DeletedFilms.cshtml", deletedFilms);
        }

        // Xóa phim (đánh dấu xóa)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var film = await _context.Films.FindAsync(id);
                if (film == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy phim" });
                }

                // Thiết lập thông tin tracking
                film.Status = "Deleted";
                film.LastAction = "Delete";
                film.ModifiedBy = User.Identity?.Name ?? SystemUser;
                film.ModificationDate = DateTime.Now;
                film.ChangeNote = "Đã xóa phim";

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Xóa phim thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi khi xóa: {ex.Message}" });
            }
        }

        // Khôi phục phim
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(int id)
        {
            try
            {
                Console.WriteLine($"Attempting to restore film with ID: {id}"); // Log for debugging

                var film = await _context.Films.FindAsync(id);
                if (film == null || film.Status != "Deleted")
                {
                    Console.WriteLine("Film not found or not deleted"); // Log for debugging
                    return Json(new { success = false, message = "Không tìm thấy phim hoặc phim chưa bị xóa" });
                }

                // Thiết lập thông tin tracking
                film.Status = "Active";
                film.LastAction = "Restore";
                film.ModifiedBy = User.Identity?.Name ?? SystemUser;
                film.ModificationDate = DateTime.Now;
                film.ChangeNote = "Khôi phục phim";

                await _context.SaveChangesAsync();

                Console.WriteLine($"Film {id} restored successfully"); // Log for debugging
                return Json(new { success = true, message = "Khôi phục thành công!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error restoring film: {ex}"); // Log full error
                return Json(new { success = false, message = $"Lỗi khi khôi phục: {ex.Message}" });
            }
        }

        // Lịch sử thay đổi
        public async Task<IActionResult> History(string searchKeyword, string actionType, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.Films
                .Where(f => f.LastAction != null)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchKeyword))
            {
                query = query.Where(f => f.Title.Contains(searchKeyword) ||
                                      f.ModifiedBy.Contains(searchKeyword));
            }

            if (!string.IsNullOrEmpty(actionType))
            {
                query = query.Where(f => f.LastAction == actionType);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(f => f.ModificationDate >= fromDate);
            }

            if (toDate.HasValue)
            {
                // Bao gồm đến hết ngày (23:59:59)
                var endOfDay = toDate.Value.Date.AddDays(1).AddSeconds(-1);
                query = query.Where(f => f.ModificationDate <= endOfDay);
            }

            var history = await query
                .OrderByDescending(f => f.ModificationDate)
                .AsNoTracking()
                .ToListAsync();

            ViewBag.SearchParameters = new { searchKeyword, actionType, fromDate, toDate };
            return View("~/Views/Film/ManagerFilm/EditHistory.cshtml", history);
        }

        // Chi tiết lịch sử
        public async Task<IActionResult> HistoryDetail(int id)
        {
            var film = await _context.Films
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == id);

            if (film == null) return NotFound();

            return PartialView("~/Views/Film/ManagerFilm/_HistoryDetailPartial.cshtml", film);
        }

        #region Helper Methods

        private async Task<List<string>> GetUniqueGenresFromDatabase()
        {
            return await _context.Films
                .Where(f => !string.IsNullOrEmpty(f.Genre) && f.Status != "Deleted")
                .Select(f => f.Genre)
                .Distinct()
                .OrderBy(g => g)
                .ToListAsync();
        }

        private async Task<List<int>> GetUniqueReleaseYearsFromDatabase()
        {
            return await _context.Films
                .Where(f => f.ReleaseDate.HasValue && f.Status != "Deleted")
                .Select(f => f.ReleaseDate.Value.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToListAsync();
        }
        private async Task<FilmFilterViewModel> GetFilterDataFromDatabase()
        {
            return new FilmFilterViewModel
            {
                Genres = await _context.Films
                    .Where(f => !string.IsNullOrEmpty(f.Genre) && f.Status != "Deleted")
                    .Select(f => f.Genre)
                    .Distinct()
                    .OrderBy(g => g)
                    .ToListAsync(),

                Statuses = await _context.Films
                    .Where(f => !string.IsNullOrEmpty(f.Status) && f.Status != "Deleted")
                    .Select(f => f.Status)
                    .Distinct()
                    .OrderBy(s => s)
                    .ToListAsync(),

                Ratings = await _context.Films
                    .Where(f => !string.IsNullOrEmpty(f.RatingValue) && f.Status != "Deleted")
                    .Select(f => f.RatingValue)
                    .Distinct()
                    .OrderBy(r => r)
                    .ToListAsync(),

                ReleaseYears = await _context.Films
                    .Where(f => f.ReleaseDate.HasValue && f.Status != "Deleted")
                    .Select(f => f.ReleaseDate.Value.Year)
                    .Distinct()
                    .OrderByDescending(y => y)
                    .Select(y => y.ToString())
                    .ToListAsync()
            };
        }

        private void SetupCreateEditViewBags()
        {
            ViewBag.Genres = GetAvailableGenres();
            ViewBag.AgeRatings = GetAgeRatingOptions();
            ViewBag.Statuses = GetStatusOptions();
        }

        private List<string> GetAvailableGenres()
        {
            return new List<string>
            {
                "Hành động", "Tình cảm", "Hài hước", "Kinh dị",
                "Viễn tưởng", "Tài liệu", "Hoạt hình", "Gia đình",
                "Khoa học viễn tưởng", "Phiêu lưu", "Thần thoại",
                "Trinh thám", "Tội phạm", "Chiến tranh", "Lịch sử"
            };
        }

        private Dictionary<string, string> GetAgeRatingOptions()
        {
            return new Dictionary<string, string>
            {
                { "P", "Mọi lứa tuổi" },
                { "C13", "13+" },
                { "C16", "16+" },
                { "C18", "18+" }
            };
        }

        private Dictionary<string, string> GetStatusOptions()
        {
            return new Dictionary<string, string>
            {
                { "Active", "Đang hoạt động" },
                { "Inactive", "Ngừng hoạt động" },
                { "Upcoming", "Sắp hoạt động" },
                { "Deleted", "Đã xóa" }
            };
        }

        private async Task<string> HandleFileUploadAsync(IFormFile file, string subFolder)
        {
            if (file == null || file.Length == 0) return null;

            var uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, UploadsFolder, subFolder);
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return $"/{UploadsFolder}/{subFolder}/{uniqueFileName}";
        }

        private void DeleteFileIfExists(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl)) return;

            var filePath = Path.Combine(_hostEnvironment.WebRootPath, fileUrl.TrimStart('/'));
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePermanent(int id)
        {
            try
            {
                var film = await _context.Films.FindAsync(id);
                if (film == null || film.Status != "Deleted")
                {
                    return Json(new { success = false, message = "Không tìm thấy phim hoặc phim chưa bị xóa" });
                }

                // Xóa poster nếu có
                if (!string.IsNullOrEmpty(film.PosterPath))
                {
                    var filePath = Path.Combine(_hostEnvironment.WebRootPath, film.PosterPath.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                _context.Films.Remove(film);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Xóa vĩnh viễn thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi khi xóa vĩnh viễn: {ex.Message}" });
            }
        }

        private string GetModifiedFields(PropertyValues original, PropertyValues current)
        {
            var changedFields = new List<string>();
            var properties = typeof(FilmsModel).GetProperties();

            foreach (var prop in properties)
            {
                if (ShouldTrackProperty(prop.Name))
                {
                    var oldValue = original[prop.Name];
                    var newValue = current[prop.Name];

                    if (!Equals(oldValue, newValue))
                    {
                        var displayAttr = prop.GetCustomAttribute<DisplayAttribute>();
                        changedFields.Add(displayAttr?.Name ?? prop.Name);
                    }
                }
            }

            return changedFields.Any() ? string.Join(", ", changedFields) : null;
        }

        private bool ShouldTrackProperty(string propName)
        {
            var ignoreList = new List<string>
    {
        nameof(FilmsModel.Id),
        nameof(FilmsModel.LastAction),
        nameof(FilmsModel.ModifiedBy),
        nameof(FilmsModel.ModificationDate),
        nameof(FilmsModel.ModifiedFields),
        nameof(FilmsModel.ChangeNote),
        nameof(FilmsModel.Packages),
        nameof(FilmsModel.PromotionFilms),
        nameof(FilmsModel.Purchases),
        nameof(FilmsModel.Rating) // Thêm dòng này để bỏ qua tracking Rating
    };
            return !ignoreList.Contains(propName);
        }
        #endregion
    }
}