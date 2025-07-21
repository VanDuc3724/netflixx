using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Netflixx.Models;
using Netflixx.Repositories;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Netflixx.Controllers
{
    //[Authorize] // Yêu cầu đăng nhập cho toàn bộ controller
    public class FeedbackController : Controller
    {
        private readonly DBContext _context;
        private readonly ILogger<FeedbackController> _logger;
        private readonly UserManager<AppUserModel> _userManager;

        public FeedbackController(
            DBContext context,
            ILogger<FeedbackController> logger,
            UserManager<AppUserModel> userManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
        }

        // Thêm hàm kiểm tra đăng nhập
        private IActionResult RedirectIfNotLoggedIn()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Redirect("/login/login"); // Redirect thủ công
            }
            return null; // Nếu đã đăng nhập, trả về null
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            //// Kiểm tra đăng nhập
            //var redirectResult = RedirectIfNotLoggedIn();
            //if (redirectResult != null) return redirectResult;
            var currentUser = await _userManager.GetUserAsync(User);

            var model = new Feedback
            {
                // Lấy thông tin người dùng từ Identity
                FullName = currentUser?.FullName ?? currentUser?.UserName,
                Email = currentUser?.Email
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(Feedback feedback)
        {
            if (!ModelState.IsValid)
            {
                return View(feedback);
            }

            try
            {
                var currentUser = await _userManager.GetUserAsync(User);

                feedback.FullName = currentUser?.FullName ?? currentUser?.UserName;
                feedback.Email = currentUser?.Email;
                feedback.CreatedAt = DateTime.Now;

                _context.Feedbacks.Add(feedback);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Cảm ơn bạn đã gửi phản hồi!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lưu phản hồi");
                ModelState.AddModelError("", "Đã xảy ra lỗi khi gửi phản hồi. Vui lòng thử lại sau.");
                return View(feedback);
            }
        }

        [HttpGet]
        public async Task<IActionResult> History(int page = 1)
        {
            // Kiểm tra đăng nhập
            var redirectResult = RedirectIfNotLoggedIn();
            if (redirectResult != null) return redirectResult;
            int pageSize = 5;
            var currentUser = await _userManager.GetUserAsync(User);
            var query = _context.Feedbacks.AsQueryable();

            // Nếu không phải admin, chỉ lấy feedback của người dùng hiện tại
            if (!User.IsInRole("Admin"))
            {
                query = query.Where(f => f.Email == currentUser.Email);
            }

            var feedbacks = await query
                .OrderByDescending(f => f.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.IsAdmin = User.IsInRole("Admin");

            return View(feedbacks);
        }

[HttpPost]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> UpdateResponse(int id, string response)
{
    var feedback = await _context.Feedbacks.FindAsync(id);
    if (feedback == null)
    {
        return NotFound();
    }

    feedback.Response = response;
    _context.Feedbacks.Update(feedback);
    await _context.SaveChangesAsync();

    return RedirectToAction(nameof(History), new { page = 1 }); // Quay lại trang đầu tiên
}
    }
}