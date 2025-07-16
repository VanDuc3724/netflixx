using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Netflixx.Models;
using Netflixx.Repositories;
using Netflixx.Services;

namespace Netflixx.Controllers
{
    public class LogoutController : Controller
    {
        private readonly SignInManager<AppUserModel> _signInManager;
        private readonly ILogger<LogoutController> _logger;
        private readonly DBContext _dbContext;
        private readonly IChatService _chatService;
        public LogoutController(
            SignInManager<AppUserModel> signInManager,
            ILogger<LogoutController> logger,
            DBContext dbContext,
            IChatService chatService)
        {
            _signInManager = signInManager;
            _logger = logger;
            _dbContext = dbContext;
            _chatService = chatService;
        }

        public IActionResult Index()
        {
            return View();
        }

        //Logout method
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;


        

            if (!string.IsNullOrEmpty(userId))
            {
                var latestLogin = await _dbContext.LoginHistory
                    .Where(h => h.UserId == userId)
                    .OrderByDescending(h => h.LoginTime)
                    .FirstOrDefaultAsync();

                if (latestLogin != null)
                {
                    latestLogin.logoutTime = DateTime.UtcNow;
                    _dbContext.LoginHistory.Update(latestLogin);
                    await _dbContext.SaveChangesAsync();
                }
    
                if (User.IsInRole("User"))
                {
                    // ===> Bây giờ dòng code này sẽ được gọi chính xác <===
                    await _chatService.HandleUserLogoutAsync(userId);
                }
            }
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync();
            await _signInManager.SignOutAsync();


            _logger.LogInformation("Đăng xuất thành công");
            return RedirectToAction("Login", "Login");
        }
    }
}
