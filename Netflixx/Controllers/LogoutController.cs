using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Netflixx.Models;

namespace Netflixx.Controllers
{
    public class LogoutController : Controller
    {
        private readonly SignInManager<AppUserModel> _signInManager;
        private readonly ILogger<LogoutController> _logger;
        private readonly UserManager<AppUserModel> _userManager;

        public LogoutController(SignInManager<AppUserModel> signInManager, ILogger<LogoutController> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
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
            await HttpContext.SignOutAsync();
            await _signInManager.SignOutAsync();
            _logger.LogInformation("Đăng xuất thành công");
            return RedirectToAction("Login", "Login");
        }
    }
}
