using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Netflixx.Models;
using Netflixx.Models.Binding;
using Netflixx.Models.ViewModel;
using Netflixx.Repositories;
using Netflixx.Services;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Netflixx.Controllers
{
    public class LoginController : Controller
    {
        private readonly UserManager<AppUserModel> _userManager;      //Quản lý người dùng (tạo, xóa, tìm kiếm...)
        private readonly SignInManager<AppUserModel> _signInManager;  //Quản lý đăng nhập/đăng xuất
        private readonly DBContext _dbContext;
        private readonly ILogger<LoginController> _logger;
        private readonly IEmailSender _emailSender;
        private readonly IOtpService _otpService;

        public LoginController(
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


        //GET Login: Hiển thị form đăng nhập, xử lý returnUrl nếu có
        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            // Truyền ReturnUrl vào ViewModel
            var model = new LoginViewModel
            {
                ReturnUrl = returnUrl ?? Url.Content("~/") // Nếu null thì mặc định về trang chủ
            };
            return View(model);
        }

        //POST Login: Xử lý đăng nhập
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            try
            {
                // 1. Xử lý ReturnUrl
                model.ReturnUrl = model.ReturnUrl ?? Url.Content("~/");

                // 2. Validate ModelState
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning($"ModelState invalid. Errors: {string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))}");
                    return View(model);
                }

                // 3. Tìm user bằng username hoặc email
                var user = await _userManager.FindByNameAsync(model.Username) ??
                          await _userManager.FindByEmailAsync(model.Username);

                if (user == null)
                {
                    _logger.LogWarning($"User not found: {model.Username}");
                    ModelState.AddModelError(string.Empty, "Tài khoản hoặc mật khẩu không đúng");
                    TempData["error"] = JsonSerializer.Serialize("Tài khoản hoặc mật khẩu không đúng");
                    return View(model);
                }

                // 4. Thử đăng nhập
                var result = await _signInManager.PasswordSignInAsync(
                    user.UserName,
                    model.Password,
                    isPersistent: model.RememberMe, // Sử dụng giá trị từ checkbox
                    lockoutOnFailure: false);

                // 5. Xử lý kết quả
                if (result.Succeeded)
                {
                    _logger.LogInformation($"User {user.UserName} logged in successfully");
                    //TempData["success"] = JsonSerializer.Serialize("Đăng nhập thành công");

                    // Thêm kiểm tra role Admin ở đây
                    if (await _userManager.IsInRoleAsync(user, "Admin"))
                    {
                        return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                    }

                    // Kiểm tra nếu ReturnUrl là trang Login thì chuyển hướng sang Home
                    if (Url.IsLocalUrl(model.ReturnUrl))
                    {
                        var isLoginUrl = model.ReturnUrl.Equals("/Login/Login", StringComparison.OrdinalIgnoreCase) ||
                                        model.ReturnUrl.Equals("/", StringComparison.OrdinalIgnoreCase);

                        if (isLoginUrl)
                        {
                            return RedirectToAction("Index", "Home");
                        }
                        return Redirect(model.ReturnUrl);
                    }
                    return RedirectToAction("Index", "Home");
                }
                else if (result.IsLockedOut)
                {
                    _logger.LogWarning($"User {user.UserName} locked out");
                    ModelState.AddModelError(string.Empty, "Tài khoản tạm thời bị khóa do đăng nhập sai quá nhiều lần");
                    return View(model);
                }
                else
                {
                    _logger.LogWarning($"Invalid password for user {user.UserName}");
                    ModelState.AddModelError(string.Empty, "Tài khoản hoặc mật khẩu không đúng");
                    TempData["error"] = JsonSerializer.Serialize("Tài khoản hoặc mật khẩu không đúng");
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi đăng nhập");
                TempData["error"] = JsonSerializer.Serialize("Đã xảy ra lỗi khi đăng nhập");
                return View(model);
            }
        }


        [HttpGet]
        //Đăng kí tài khoản
        public IActionResult SignUp()
        {
            return View();
        }


        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignUp(SignUpViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                _logger.LogWarning($"ModelState invalid: {string.Join(", ", errors)}");
                return View(model);
            }

            // Kiểm tra OTP trước khi đăng ký
            if (!_otpService.ValidateOtp(model.Email, model.Otp))
            {
                ModelState.AddModelError("Otp", "Mã OTP không hợp lệ hoặc đã hết hạn");
                _logger.LogWarning($"OTP không hợp lệ cho email: {model.Email}");
                TempData["error"] = JsonSerializer.Serialize("Mã OTP không hợp lệ hoặc đã hết hạn");
                return View(model);
            }

            // Kiểm tra email đã tồn tại chưa (thêm để chắc chắn)
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            var existingUsername = await _userManager.FindByNameAsync(model.UserName);
            if (existingUser != null || existingUsername != null)
            {
                ModelState.AddModelError("Email", "Email hoặc username đã được sử dụng");
                TempData["error"] = JsonSerializer.Serialize("Email hoặc username đã được sử dụng");
                return View(model);
            }

            var user = new AppUserModel
            {
                UserName = model.UserName,
                Email = model.Email,
                FullName = model.FullName,

            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // create separate account record for this user
                var account = new UserAccountsModel
                {
                    UserID = user.Id,
                    Balance = 0,
                    PointsBalance = 0
                };
                _dbContext.UserAccounts.Add(account);
                await _dbContext.SaveChangesAsync();

                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("SignUpSuccess"); // Chuyển hướng đến trang thành công
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
                _logger.LogError($"Lỗi đăng ký: {error.Description}");
                TempData["error"] = JsonSerializer.Serialize("Lỗi đăng kí");
            }

            // Xử lý đăng ký...
            return RedirectToAction("SignUpSuccess");
        }


        // GET: Trang thành công
        [HttpGet]
        public IActionResult SignUpSuccess()
        {
            return View();
        }


        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> SendSignUpOtp([FromBody] EmailRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { success = false, message = "Request không hợp lệ" });
                }

                // Kiểm tra email hợp lệ
                if (!new EmailAddressAttribute().IsValid(request.Email))
                {
                    return BadRequest(new { success = false, message = "Email không hợp lệ" });
                }

                // Kiểm tra số lần gửi OTP
                var otpAttempts = _otpService.GetOtpAttempts(request.Email);
                if (otpAttempts >= 5)
                {
                    return Ok(new
                    {
                        success = false,
                        message = $"Bạn đã yêu cầu OTP quá 5 lần. Vui lòng thử lại sau {_otpService.GetRemainingLockTime(request.Email)} phút.",
                        isLocked = true
                    });
                }

                // Kiểm tra email đã tồn tại
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user != null)
                {
                    return Ok(new { success = false, message = "Email đã được sử dụng" });
                }

                // Tạo và gửi OTP
                var otp = _otpService.GenerateOtp(request.Email);
                await _emailSender.SendEmailAsync(
                    request.Email,
                    "Mã OTP đăng ký Netflixx",
                    $"Mã OTP của bạn là: <strong>{otp}</strong>. Mã có hiệu lực trong 5 phút.");

                return Ok(new
                {
                    success = true,
                    attemptsLeft = 5 - (otpAttempts + 1)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi OTP");
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống" });
            }
        }


        // GET: Giao diện nhập email để gửi OTP
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: Xử lý yêu cầu gửi OTP
        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Không tiết lộ user không tồn tại
                return RedirectToAction("ForgotPasswordConfirmation");
            }

            // Tạo và gửi OTP
            var otp = _otpService.GenerateOtp(model.Email);

            // Gửi email chứa OTP
            await _emailSender.SendEmailAsync(
                model.Email,
                "Mã OTP đặt lại mật khẩu",
                $"Mã OTP của bạn là: {otp}. Mã có hiệu lực trong 5 phút.");

            return RedirectToAction("VerifyOtp", new { email = model.Email });
        }

        // GET: Giao diện nhập OTP
        [HttpGet]
        public IActionResult VerifyOtp(string email)
        {
            var model = new VerifyOtpViewModel { Email = email };
            return View(model);
        }

        // POST: Xác thực OTP
        [HttpPost]
        public IActionResult VerifyOtp(VerifyOtpViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new
                {
                    success = false,
                    message = "Vui lòng nhập mã OTP hợp lệ."
                });
            }

            if (string.IsNullOrEmpty(model.Email))
            {
                return Json(new
                {
                    success = false,
                    message = "Phiên làm việc đã hết hạn. Vui lòng thử lại."
                });
            }

            if (!_otpService.ValidateOtp(model.Email, model.Otp))
            {
                _logger.LogWarning($"OTP không hợp lệ cho email: {model.Email}");
                return Json(new
                {
                    success = false,
                    message = "Mã OTP không hợp lệ hoặc đã hết hạn!"
                });
            }

            HttpContext.Session.SetString("OTP_Verified", model.Email);
            return Json(new
            {
                success = true,
                redirectUrl = Url.Action("ResetPassword", new { email = model.Email })
            });
        }


        [HttpPost]
        public async Task<IActionResult> ResendOtp(string email)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    return Json(new { success = false, message = "Email không hợp lệ" });
                }

                // Kiểm tra email có tồn tại trong hệ thống không (cho chức năng quên mật khẩu)
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    return Json(new { success = false, message = "Email không tồn tại trong hệ thống" });
                }

                // Gửi OTP mới
                var otp = _otpService.GenerateOtp(email);
                await _emailSender.SendEmailAsync(
                    email,
                    "Mã OTP đặt lại mật khẩu",
                    $"Mã OTP mới của bạn là: {otp}. Mã có hiệu lực trong 5 phút.");

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi lại OTP");
                return Json(new { success = false, message = "Đã xảy ra lỗi hệ thống" });
            }
        }


        // GET: Giao diện đặt lại mật khẩu
        [HttpGet]
        public IActionResult ResetPassword(string email)
        {
            // Kiểm tra xác thực OTP
            if (HttpContext.Session.GetString("OTP_Verified") != email)
            {
                return RedirectToAction("ForgotPassword");
            }

            var model = new ResetPasswordViewModel { Email = email };
            return View(model);
        }

        // POST: Xử lý đặt lại mật khẩu
        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Kiểm tra xác thực OTP
            if (HttpContext.Session.GetString("OTP_Verified") != model.Email)
            {
                return RedirectToAction("ForgotPassword");
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return RedirectToAction("ForgotPasswordConfirmation");
            }

            // Đặt lại mật khẩu
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, model.Password);

            if (result.Succeeded)
            {
                // Xóa session sau khi đổi mật khẩu thành công
                HttpContext.Session.Remove("OTP_Verified");
                return RedirectToAction("ResetPasswordConfirmation");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        // Các action confirmation
        public IActionResult ForgotPasswordConfirmation() => View();
        public IActionResult ResetPasswordConfirmation() => View();

    }


}
