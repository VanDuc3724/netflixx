using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Netflixx.Models;
using Netflixx.Models.ViewModel;
using Netflixx.Repositories;
using Netflixx.Services;

namespace Netflixx.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<AppUserModel> _userManager;      //Quản lý người dùng (tạo, xóa, tìm kiếm...)
        private readonly SignInManager<AppUserModel> _signInManager;  //Quản lý đăng nhập/đăng xuất
        private readonly DBContext _dbContext;
        private readonly ILogger<LoginController> _logger;
        private readonly IEmailSender _emailSender;
        private readonly IOtpService _otpService;

        public AccountController(
            UserManager<AppUserModel> userManager,
            SignInManager<AppUserModel> signInManager,
            DBContext dbContext,
            ILogger<LoginController> logger,
            IEmailSender emailSender,
            IOtpService otpService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _dbContext = dbContext;
            _logger = logger;
            _emailSender = emailSender;
            _otpService = otpService;
        }

        // GET: Giao diện đổi mật khẩu (sau khi xác thực OTP)
        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            // Kiểm tra xác thực OTP
            if (HttpContext.Session.GetString("ChangePassword_OTP_Verified") != "true")
            {
                return RedirectToAction("Profile", "Home");
            }

            var user = _userManager.GetUserAsync(User).Result;
            var model = new ChangePasswordViewModel { Email = user?.Email };
            return View(model);
        }

        // POST: Xử lý đổi mật khẩu
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Kiểm tra xác thực OTP
            if (HttpContext.Session.GetString("ChangePassword_OTP_Verified") != "true")
            {
                return RedirectToAction("Profile", "Home");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            // Tạo token reset password (vì không cần mật khẩu cũ)
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

            if (result.Succeeded)
            {
                // Xóa session sau khi đổi mật khẩu thành công
                HttpContext.Session.Remove("ChangePassword_OTP_Verified");

                await _signInManager.RefreshSignInAsync(user);
                _logger.LogInformation("User changed their password successfully.");

                TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
                return RedirectToAction("Profile", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // Thêm vào AccountController.cs

        // POST: Gửi OTP đổi mật khẩu
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> SendChangePasswordOtp()
        {
            try
            {
                // Lấy thông tin user hiện tại
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                // Kiểm tra số lần gửi OTP
                var otpAttempts = _otpService.GetOtpAttempts(user.Email);
                if (otpAttempts >= 5)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Bạn đã yêu cầu OTP quá 5 lần. Vui lòng thử lại sau {_otpService.GetRemainingLockTime(user.Email)} phút.",
                        isLocked = true
                    });
                }

                // Tạo và gửi OTP
                var otp = _otpService.GenerateOtp(user.Email);
                await _emailSender.SendEmailAsync(
                    user.Email,
                    "Mã OTP đổi mật khẩu Netflixx",
                    $"Mã OTP của bạn là: <strong>{otp}</strong>. Mã có hiệu lực trong 5 phút.");

                return Json(new
                {
                    success = true,
                    redirectUrl = Url.Action("VerifyChangePasswordOtp"),
                    attemptsLeft = 5 - (otpAttempts + 1)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending change password OTP");
                return Json(new { success = false, message = "Đã xảy ra lỗi hệ thống" });
            }
        }

        // GET: Giao diện xác nhận OTP đổi mật khẩu
        [HttpGet]
        [Authorize]
        public IActionResult VerifyChangePasswordOtp()
        {
            var user = _userManager.GetUserAsync(User).Result;
            var model = new VerifyOtpViewModel { Email = user?.Email };
            return View(model);
        }

        // POST: Xác thực OTP đổi mật khẩu
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> VerifyChangePasswordOtp(VerifyOtpViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new
                {
                    success = false,
                    message = "Vui lòng nhập mã OTP hợp lệ."
                });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new
                {
                    success = false,
                    message = "User not found"
                });
            }

            if (!_otpService.ValidateOtp(user.Email, model.Otp))
            {
                _logger.LogWarning($"Invalid OTP for email: {user.Email}");
                return Json(new
                {
                    success = false,
                    message = "Mã OTP không hợp lệ hoặc đã hết hạn!"
                });
            }

            HttpContext.Session.SetString("ChangePassword_OTP_Verified", "true");
            return Json(new
            {
                success = true,
                redirectUrl = Url.Action("ChangePassword")
            });
        }
    }
}
