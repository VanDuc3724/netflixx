using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Netflixx.Areas.Admin.Extensions;
using Netflixx.Models;
using Netflixx.Repositories;
using Netflixx.Services;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting; // Add this
using System.IO;                   // Add this
using System;                      // Add this for Guid, DateTime
using System.Linq;                 // Add this for Linq
using Microsoft.Extensions.Logging; // Add this
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System.Security.Cryptography.Xml;
using Netflixx.Models.Binding;
using System.Diagnostics.Eventing.Reader;
[Area("Admin")]

public class UserController : Controller
{

    private readonly DBContext _context;
    private readonly UserManager<AppUserModel> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<UserController> _logger;
    private readonly IAvatarService _avatarService;
    private readonly IWebHostEnvironment _env;
    private readonly IEmailSender _emailSender;
    public UserController(
      ILogger<UserController> logger,
        DBContext context,
        UserManager<AppUserModel> userManager,
        RoleManager<IdentityRole> roleManager,
        IAvatarService avatarService,
        IWebHostEnvironment env,
        IEmailSender emailSender
        )
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _avatarService = avatarService;
        _env = env;
        _emailSender = emailSender;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }
    [HttpGet]
    public IActionResult LockedList()
    {
        return View("LockedUSer");
    }
    [HttpGet]
    public async Task<IActionResult> GetLockedUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pagesize = 10,
        [FromQuery] string search = null,
        [FromQuery] DateTime? lockDateStart = null,
        [FromQuery] DateTime? lockDateEnd = null,
        [FromQuery] string role = null)
    {
        // Query cơ bản: CHỈ lấy các user đang bị khóa
        var usersQuery = _context.Users
            .Where(u => u.LockoutEnabled && u.LockoutEnd > DateTimeOffset.UtcNow);

        // Áp dụng bộ lọc tìm kiếm
        if (!string.IsNullOrEmpty(search))
        {
            var keyword = search.ToLower().Trim();
            usersQuery = usersQuery.Where(u =>
                u.FullName.ToLower().Contains(keyword) ||
                u.Email.ToLower().Contains(keyword));
        }

        // Áp dụng bộ lọc ngày tháng
        if (lockDateStart.HasValue)
        {
            usersQuery = usersQuery.Where(u => u.LockoutEnd >= lockDateStart.Value.ToUniversalTime());
        }

        if (lockDateEnd.HasValue)
        {
            var endDate = lockDateEnd.Value.AddDays(1).ToUniversalTime(); // bao gồm cả ngày kết thúc
            usersQuery = usersQuery.Where(u => u.LockoutEnd < endDate);
        }

        // Áp dụng bộ lọc vai trò
        if (!string.IsNullOrEmpty(role))
        {
            var roleId = await _context.Roles
                .Where(r => r.Name == role)
                .Select(r => r.Id).FirstOrDefaultAsync();

            if (roleId != null)
            {
                usersQuery = usersQuery
                    .Where(u => _context.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == roleId));
            }
        }

        // Phân trang và định dạng dữ liệu trả về
        var totalCount = await usersQuery.CountAsync();
        var totalPage = (int)Math.Ceiling((double)totalCount / pagesize);

        var usersPage = await usersQuery
            .OrderByDescending(u => u.LockoutEnd) // Sắp xếp theo ngày bị khóa gần nhất
            .Skip((page - 1) * pagesize)
            .Take(pagesize)
            .ToListAsync();

        // Lấy vai trò cho user trong trang
        var userIdsInPage = usersPage.Select(u => u.Id).ToList();
        var rolesDict = await (from userRole in _context.UserRoles
                               where userIdsInPage.Contains(userRole.UserId)
                               join r in _context.Roles on userRole.RoleId equals r.Id
                               select new { userRole.UserId, r.Name })
                            .ToDictionaryAsync(x => x.UserId, x => x.Name);

        // Định dạng dữ liệu cuối cùng
        var resultItems = usersPage.Select(u => new
        {
            Id = u.Id,
            FullName = u.FullName,
            Email = u.Email,
            Role = rolesDict.GetValueOrDefault(u.Id, "Chưa gán"),
            LockDate = u.LockoutEnd?.ToLocalTime().ToString("dd/MM/yyyy HH:mm"), // Format lại ngày tháng
            AvatarUrl = u.AvatarUrl
        }).ToList();

        var result = new
        {
            TotalCount = totalCount,
            TotalPage = totalPage,
            Page = page,
            PageSize = pagesize,
            Items = resultItems
        };

        return Ok(result);
    }

    // 2. Hàm để MỞ KHÓA hàng loạt
    [HttpPost]
    public async Task<IActionResult> BulkUnlockUsers([FromBody] List<string> userIds)
    {
        if (userIds == null || !userIds.Any())
        {
            return BadRequest(new { success = false, message = "Không có người dùng nào được chọn." });
        }

        int successCount = 0;
        var errorMessages = new List<string>();

        foreach (var userId in userIds)
        {
            var userToUnlock = await _userManager.FindByIdAsync(userId);
            if (userToUnlock == null)
            {
                errorMessages.Add($"Không tìm thấy ID: {userId}");
                continue;
            }

            if (userToUnlock.LockoutEnd > DateTimeOffset.UtcNow)
            {
                var result = await _userManager.SetLockoutEndDateAsync(userToUnlock, null);
                if (result.Succeeded)
                {
                    await _userManager.ResetAccessFailedCountAsync(userToUnlock);
                    await _userManager.SetLockoutEnabledAsync(userToUnlock, false); // Tắt hẳn chế độ Lockout
                    successCount++;
                }
                else
                {
                    errorMessages.Add($"Lỗi mở khóa {userToUnlock.Email}: {string.Join(",", result.Errors.Select(e => e.Description))}");
                }
            }
        }

        if (successCount > 0)
        {
            var message = $"Đã mở khóa thành công {successCount} tài khoản.";
            if (errorMessages.Any())
            {
                message += " Các lỗi gặp phải: " + string.Join("; ", errorMessages);
            }
            return Ok(new { success = true, message = message });
        }

        return BadRequest(new { success = false, message = $"Thao tác thất bại. Lỗi: {string.Join("; ", errorMessages)}" });
    }



    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmAdminPassword([FromBody] string password) // Nhận trực tiếp chuỗi từ body
    {
        // Kiểm tra thủ công xem mật khẩu có được gửi hay không
        if (string.IsNullOrEmpty(password))
        {
            return BadRequest(new { success = false, message = "Vui lòng nhập mật khẩu." });
        }
        
        var adminUser = await _userManager.GetUserAsync(User);
        if (adminUser == null)
        {
            return Unauthorized(new { success = false, message = "Không tìm thấy người dùng quản trị." });
        }

        // Sử dụng trực tiếp `password` (là chuỗi mật khẩu người dùng nhập)
        var passwordCorrect = await _userManager.CheckPasswordAsync(adminUser, password);

        if (passwordCorrect)
        {
            HttpContext.Session.SetString("AdminLastAuthTimeUtc", DateTime.UtcNow.ToString("o"));
            _logger.LogInformation("Quản trị viên {AdminUserId} đã xác thực lại mật khẩu thành công lúc {Time}.", adminUser.Id, DateTime.UtcNow);
            return Json(new { success = true, message = "Xác thực thành công." });
        }
        else
        {   
            _logger.LogWarning("Quản trị viên {AdminUserId} xác thực lại mật khẩu thất bại.", adminUser.Id);
            return Json(new { success = false, message = "Mật khẩu quản trị viên không đúng." });
        }
    }
    [HttpGet]
    public IActionResult StaticCount()
    {
        var totalUsers = _context.Users.Count();
        var activeUsers = _context.Users.Count(u => !(u.LockoutEnabled && u.LockoutEnd > DateTime.UtcNow));
        var newUsers = _context.Users.Count(u => u.RegisteredAt >= DateTime.UtcNow.AddDays(-7));
        var totalPaidUsers = _context.Users.Count(u => u.PackageSubscriptions.Any(ps => ps.Status == "active" && ps.EndDate > DateTime.UtcNow));
        var items = new
        {
            TotalCount = totalUsers,
            ActiveCount = activeUsers,
            NewThisWeekCount = newUsers,
            TotalPaid = totalPaidUsers
        };
        return Ok(items);
    }



    [HttpGet]
    public IActionResult getUsers([FromQuery] int page = 1, [FromQuery] int pagesize = 10, [FromQuery] string search = null)
    {
        var usersQuery = _context.Users
            .Where(u => !u.LockoutEnabled || (u.LockoutEnd == null || u.LockoutEnd <= DateTime.UtcNow));

        if (!string.IsNullOrEmpty(search))
        {
            var keyword = search.ToLower().Trim();
            usersQuery = usersQuery.Where(u =>
                u.FullName.ToLower().Contains(keyword) ||
                u.Email.ToLower().Contains(keyword) ||
                u.UserName.ToLower().Contains(keyword));
        }

        var totalCount = usersQuery.Count();
        var totalPage = (int)Math.Ceiling((double)totalCount / pagesize);

        var usersPage = usersQuery
            .OrderBy(u => u.FullName)
            .Skip((page - 1) * pagesize)
            .Take(pagesize)
            .ToList();

        // Lấy danh sách ID người dùng trong trang hiện tại
        var userIds = usersPage.Select(u => u.Id).ToList();

        // Truy vấn bản ghi đăng xuất gần nhất cho các user trong trang
        var lastLogoutDict = _context.LoginHistory
            .Where(lh => userIds.Contains(lh.UserId) && lh.logoutTime != null)
            .GroupBy(lh => lh.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                LastLogout = g.OrderByDescending(x => x.logoutTime).FirstOrDefault()
            })
            .ToDictionary(x => x.UserId, x => x.LastLogout);

        var nowUtc = DateTime.UtcNow;

        var resultItems = usersPage.Select(u =>
        {
            string statusText = "Offline";

            if (lastLogoutDict.TryGetValue(u.Id, out var lastLogout) && lastLogout != null)
            {
                TimeSpan sinceLogout = nowUtc - lastLogout.logoutTime.ToUniversalTime();

                var initialLoginCondition = Math.Abs((lastLogout.logoutTime - lastLogout.LoginTime).TotalSeconds) < 1;

                if (sinceLogout.TotalMinutes <= 5 || initialLoginCondition)
                {
                    statusText = "Online";
                }
            }
            else
            {
                // Không có bản ghi logout nào ⇒ xem như offline
                statusText = "Offline";
            }

            return new
            {
                u.Id,
                u.FullName,
                u.Email,
                u.UserName,
                Status = statusText,
                CreatedAt = u.RegisteredAt.ToString("dd/MM/yyyy"),
                Role = _userManager.GetRolesAsync(u).Result.FirstOrDefault() ?? "Chưa có vai trò",
                AvatarUrl = u.AvatarUrl
            };
        }).ToList();

        var result = new
        {
            TotalCount = totalCount,
            TotalPage = totalPage,
            Page = page,
            PageSize = pagesize,
            Items = resultItems
        };

        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> CreateUser()
    {

        return PartialView("_CreateUser");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser([FromForm] UserViewModel model)
    {
        // . Kiểm tra email đã tồn tại chưa
        var existing = await _userManager.FindByEmailAsync(model.Email);

        if (existing != null)
        {
            return BadRequest(new
            {
                isValid = false,
                errors = new[] { "Email này đã được sử dụng." }
            });
        }
        // . Validate các rule custom
        var (isValid, errors) = UserExtenstions.ValidateUser(model);
        if (!isValid)
        {
            // Trả về 400 kèm danh sách lỗi
            return BadRequest(new
            {
                isValid = false,
                errors = errors
            });
        }
        string finalUserAvatarUrl = null; // This will be the AvatarUrl for the NEW user
        string adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // 3. Tạo AppUserModel mới
        var user = new AppUserModel
        {
            UserName = model.Email.Trim(),
            Email = model.Email.Trim(),
            FullName = model.Name.Trim(),
            PhoneNumber = model.PhoneNumber,
            Address = model.Address,
            DateOfBirth = model.dateOfBirth

        };

        // 4. Tạo user với password
        var createResult = await _userManager.CreateAsync(user, model.Password);
        if (!createResult.Succeeded)
        {
            // Gom tất cả lỗi do Identity trả về
            var identityErrors = createResult.Errors.Select(e => e.Description).ToList();
            return BadRequest(new
            {
                isValid = false,
                errors = identityErrors
            });
        }

        if (!string.IsNullOrEmpty(model.AvatarUrl))
        {
            string sourceAvatarRelativePath = model.AvatarUrl.TrimStart('/');
            if (sourceAvatarRelativePath.StartsWith("img/avatar/default/", StringComparison.OrdinalIgnoreCase))
            {
                finalUserAvatarUrl = model.AvatarUrl;
            }
            else if (sourceAvatarRelativePath.StartsWith($"img/avatar/upload/{adminUserId}/", StringComparison.OrdinalIgnoreCase))
            {
                string fileName = Path.GetFileName(sourceAvatarRelativePath);
                string newUserAvatarSubFolder = $"img/avatar/upload/{user.Id}";
                string newUserPhysicalUploadDir = Path.Combine(_env.WebRootPath, newUserAvatarSubFolder);

                if (!Directory.Exists(newUserPhysicalUploadDir))
                {
                    Directory.CreateDirectory(newUserPhysicalUploadDir);
                }

                string sourcePhysicalPath = Path.Combine(_env.WebRootPath, sourceAvatarRelativePath);
                string destinationPhysicalPath = Path.Combine(newUserPhysicalUploadDir, fileName);
                try
                {
                    if (System.IO.File.Exists(sourcePhysicalPath))
                    {
                        System.IO.File.Copy(sourcePhysicalPath, destinationPhysicalPath, true); // Overwrite if exists (shouldn't for new user)
                        finalUserAvatarUrl = $"/{newUserAvatarSubFolder}/{fileName}"; // Relative URL for the new user
                        _logger.LogInformation("Copied avatar from admin storage {SourcePath} to new user {UserId} storage {DestinationPath}. Final URL: {FinalUrl}", sourcePhysicalPath, user.Id, destinationPhysicalPath, finalUserAvatarUrl);
                    }
                    else
                    {
                        _logger.LogWarning("Source avatar file not found for copying: {SourcePath}. New user {UserId} will not have this avatar.", sourcePhysicalPath, user.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error copying avatar from {SourcePath} to {DestinationPath} for new user {UserId}.", sourcePhysicalPath, destinationPhysicalPath, user.Id);
                    // Decide if this is a critical error. For now, user is created, avatar assignment failed.
                }
            }
            else
            {
                _logger.LogWarning("Received AvatarUrl '{AvatarUrl}' for new user {UserId} that is not a recognized default or admin-uploaded path. Avatar will not be set.", model.AvatarUrl, user.Id);
            }
        }
        else
        {
            _logger.LogInformation("No AvatarUrl provided for new user {UserId}. Avatar will be null.", user.Id);
        }

        // Update the user with the final avatar URL if it was determined
        if (!string.IsNullOrEmpty(finalUserAvatarUrl))
        {
            user.AvatarUrl = finalUserAvatarUrl;
            var updateAvatarResult = await _userManager.UpdateAsync(user);
            if (!updateAvatarResult.Succeeded)
            {
                _logger.LogError("Failed to update AvatarUrl for user {UserId}. Errors: {Errors}", user.Id, string.Join(", ", updateAvatarResult.Errors.Select(e => e.Description)));
                // Log errors, but proceed since user creation was successful.
            }
        }
        // 5. Gán role
        if (!await _roleManager.RoleExistsAsync(model.Role))
            await _roleManager.CreateAsync(new IdentityRole(model.Role));
        await _userManager.AddToRoleAsync(user, model.Role);


        try
        {
            var subject = "Chào mừng bạn đến với Netflixx!";

            // Tạo nội dung email (bạn có thể dùng HTML để làm đẹp hơn)
            var message = $@"
            <html>
            <body>
                <h2>Xin chào {user.FullName},</h2>
                <p>Tài khoản của bạn tại Netflixx đã được tạo thành công!</p>
                <p>Dưới đây là thông tin đăng nhập của bạn:</p>
                <ul>
                    <li><strong>Email (Tên đăng nhập):</strong> {user.Email}</li>
                    <li><strong>Mật khẩu:</strong> {model.Password} <em>(Mật khẩu bạn của bạn)</em></li>
                </ul>
                <p>Bạn có thể đăng nhập ngay bây giờ và bắt đầu trải nghiệm dịch vụ của chúng tôi.</p>
                <p>Trân trọng,<br/>Đội ngũ Netflixx</p>
            </body>
            </html>";

            await _emailSender.SendEmailAsync(user.Email, subject, message);

            _logger.LogInformation("Email chào mừng đã được gửi thành công đến {Email}", user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gửi email chào mừng đến {Email} thất bại.", user.Email);
        }


        // 6. Thành công
        return Ok(new
        {
            isValid = true,
            message = "Thêm người dùng thành công!", // Add success message
            errors = Array.Empty<string>()
        });
    }


    [HttpPost] // Route cho action này sẽ là /api/Email/Send
    public async Task<IActionResult> SendEmail([FromBody] SendEmailRequest emailData)
    {
        if (emailData == null)
        {
            return BadRequest("Dữ liệu email không hợp lệ.");
        }
        await _emailSender.SendEmailAsync(
           emailData.To, emailData.Subject, emailData.Body
        );

        return Ok(new { message = "Email đã được gửi thành công!" });
    }




    [HttpGet]
    public IActionResult OpenDetail()
    {

        return PartialView("_UserDetail");
    }


    [HttpGet]
    public async Task<IActionResult> getUSerDetail(string ID)
    {
        var user = await _userManager.FindByIdAsync(ID);
        if (user == null) return NotFound();

        var userStatus = new UserStatus
        {
            UserID = ID
        };
        var now = DateTimeOffset.UtcNow;
        var end = user.LockoutEnd;
        if (end.HasValue && end <= now)
        {
            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);
            await _userManager.SetLockoutEnabledAsync(user, false);
            end = null;
        }
        if (end == DateTimeOffset.MaxValue)
        {
            userStatus.Code = UserStatusCode.PermanentLocked;
            userStatus.Text = "Đã bị khóa vĩnh viễn.";
            userStatus.LockEndDate = null;
        }
        else if (end.HasValue && end > now)
        {
            userStatus.Code = UserStatusCode.Locked;
            userStatus.LockEndDate = user.LockoutEnd;

            TimeSpan remainingTime = user.LockoutEnd.Value - DateTimeOffset.UtcNow;

            if (remainingTime.TotalDays >= 1)
            {
                userStatus.Text = $"Đã bị khóa đến {user.LockoutEnd.Value.ToLocalTime():dd/MM/yyyy HH:mm}. Còn lại {(int)Math.Ceiling(remainingTime.TotalDays)} ngày.";
            }
            else if (remainingTime.TotalHours >= 1)
            {
                userStatus.Text = $"Đã bị khóa đến {user.LockoutEnd.Value.ToLocalTime():HH:mm}. Còn lại {(int)Math.Ceiling(remainingTime.TotalHours)} giờ.";
            }
            else if (remainingTime.TotalMinutes >= 1)
            {
                userStatus.Text = $"Đã bị khóa đến {user.LockoutEnd.Value.ToLocalTime():HH:mm}. Còn lại {(int)Math.Ceiling(remainingTime.TotalMinutes)} phút.";
            }
            else
            {
                userStatus.Text = $"Đã bị khóa tạm thời. Sẽ hết khóa rất sớm.";
            }
        }

        else
        {
            var lastActivity = await _context.LoginHistory



                .Where(lh => lh.UserId == ID && lh.logoutTime != null)
                .OrderByDescending(lh => lh.logoutTime)
                .FirstOrDefaultAsync();

            if (lastActivity != null)
            {
                // Xác định thời gian kể từ lần cuối cùng người dùng đăng xuất
                TimeSpan timeSinceLastLogout = DateTime.UtcNow - lastActivity.logoutTime.ToUniversalTime();
                var initialLoginCondition = Math.Abs((lastActivity.logoutTime - lastActivity.LoginTime).TotalSeconds) < 1;
                if (timeSinceLastLogout.TotalMinutes <= 5 || initialLoginCondition) // Có thể điều chỉnh ngưỡng này
                {
                    userStatus.Code = UserStatusCode.Active;
                    userStatus.Text = "Đang hoạt động";
                    userStatus.LastOnTime = null;
                }
                else
                {
                    userStatus.Code = UserStatusCode.OffLine;
                    userStatus.LastOnTime = lastActivity.logoutTime;
                    if (timeSinceLastLogout.TotalMinutes < 60)
                    {
                        userStatus.Text = $"Offline {(int)Math.Floor(timeSinceLastLogout.TotalMinutes)} phút trước";
                    }
                    else if (timeSinceLastLogout.TotalHours < 24)
                    {
                        userStatus.Text = $"Offline {(int)Math.Floor(timeSinceLastLogout.TotalHours)} giờ trước";
                    }
                    else
                    {
                        userStatus.Text = $"Offline vào {lastActivity.logoutTime.ToLocalTime():dd/MM/yyyy HH:mm}";
                    }
                }
            }
            else
            {
                // Không có dữ liệu đăng nhập/đăng xuất hoặc bản ghi đăng xuất không có giá trị
                userStatus.Code = UserStatusCode.OffLine;
                userStatus.Text = "Không có dữ liệu hoạt động gần đây";
                userStatus.LastOnTime = null;
            }
        }

        var userDetail = new UserDetailModel
        {
            ID = user.Id,
            Name = user.FullName,
            Email = user.Email,
            AvatarUrl = user.AvatarUrl,
            Role = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "Chưa có vai trò",
            DateOfBirth = user.DateOfBirth,
            RegisterAt = user.RegisteredAt,
            //MovieRateCount = await _context.Users.Where(fc => fc.== ID).CountAsync(), // Đếm số lượng bình luận của người dùng này
            UserStatus = userStatus,
            Address = user.Address,
            PhoneNumber = user.PhoneNumber,
            Blogs = await _context.BlogPosts.Where(bl => bl.AuthorId == ID).ToListAsync(),
            FavoriteFilms = await _context.FavoriteFilms.Where(f => f.UserID == ID).ToListAsync()
        };

        return Ok(userDetail);
    }
    [HttpPost]
    public async Task<IActionResult> LockToggle([FromBody] UserStatus model)
    {

        if (model == null || string.IsNullOrEmpty(model.UserID))
        {
            return BadRequest(new { success = false, message = "Dữ liệu yêu cầu không hợp lệ. Vui lòng cung cấp UserID." });
        }


        var user = await _userManager.FindByIdAsync(model.UserID);
        if (user == null)
        {
            return NotFound(new { success = false, message = "Không tìm thấy người dùng với ID đã cung cấp." });
        }

        IdentityResult result;
        string actionMessage;
        switch (model.Code)
        {
            case UserStatusCode.Locked:
                if (!user.LockoutEnabled)
                {
                    result = await _userManager.SetLockoutEnabledAsync(user, true);
                    if (!result.Succeeded)
                    {
                        return StatusCode(500, new { success = false, message = "Lỗi khi bật tính năng khóa tài khoản. Vui lòng thử lại." });
                    }
                }
                result = await _userManager.SetLockoutEndDateAsync(user, model.LockEndDate);
                actionMessage = $"Tài khoản {user.Email} đã được khóa đến {model.LockEndDate.Value.ToLocalTime():dd/MM/yyyy HH:mm}.";


                if (!result.Succeeded)
                {
                    return StatusCode(500, new { success = false, message = $"Không thể khóa tài khoản: {string.Join(", ", result.Errors.Select(e => e.Description))}" });
                }
                break;

            case UserStatusCode.PermanentLocked:
                result = await _userManager.SetLockoutEnabledAsync(user, true);
                if (!result.Succeeded)
                {
                    return StatusCode(500, "lỗi khóa tài khoản");
                }
                result = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                actionMessage = $"Tài khoản {user.Email} đã được khóa vĩnh viễn.";
                break;

            default:
                if (!user.LockoutEnabled)
                    await _userManager.SetLockoutEnabledAsync(user, true);

                result = await _userManager.SetLockoutEndDateAsync(user, null);
                if (!result.Succeeded)
                {
                    return StatusCode(500, new { success = false, message = $"Không thể mở khóa tài khoản: {string.Join(", ", result.Errors.Select(e => e.Description))}" });
                }
                result = await _userManager.ResetAccessFailedCountAsync(user);
                if (!result.Succeeded)
                {
                    return StatusCode(500, new { success = false, message = $"Không thể mở khóa tài khoản: {string.Join(", ", result.Errors.Select(e => e.Description))}" });
                }
                result = await _userManager.SetLockoutEnabledAsync(user, false);
                if (!result.Succeeded)
                {
                    return StatusCode(500, new { success = false, message = $"Không thể mở khóa tài khoản: {string.Join(", ", result.Errors.Select(e => e.Description))}" });
                }
                actionMessage = $"Tài khoản {user.Email} đã được mở khóa thành công.";
                break;
        }



        return Ok(new { success = true, message = actionMessage });
    }


    [HttpPost]
    public async Task<IActionResult> UpdateUser([FromBody] UserDetailModel model)
    {
        if (model == null)
        {
            return BadRequest("Dữ liệu người dùng không hợp lệ.");
        }

        if (string.IsNullOrWhiteSpace(model.ID))
        {
            return BadRequest("ID người dùng không được để trống.");
        }

        var userVm = new UserViewModel
        {
            Id = model.ID,
            Role = model.Role,
            Name = model.Name,
            Email = model.Email,
            Password = model.Password,
            PhoneNumber = model.PhoneNumber,
            Address = model.Address,
            dateOfBirth = model.DateOfBirth
        };
        var (isValid, errors) = UserExtenstions.ValidateUserForUpdate(userVm);
        if (!isValid)
        {
            foreach (var err in errors)
                ModelState.AddModelError("", err);
            return BadRequest(ModelState);
        }

        var userToUpdate = await _userManager.FindByIdAsync(model.ID);
        if (userToUpdate == null)
        {
            return NotFound("Không tìm thấy người dùng.");
        }
        if (!string.IsNullOrWhiteSpace(model.Email) 
            && !model.Email.Equals(userToUpdate.Email, StringComparison.OrdinalIgnoreCase))
        {
            var emailOwner = await _userManager.FindByEmailAsync(model.Email);
            if (emailOwner != null)
            {
                // Nếu tìm thấy 1 user khác đang dùng email này
                return BadRequest($"Email “{model.Email}” đã có người sử dụng.");
            }
        }
        userToUpdate.FullName = model.Name;
        userToUpdate.Email = model.Email;
        userToUpdate.PhoneNumber = model.PhoneNumber;
        userToUpdate.DateOfBirth = model.DateOfBirth;
        userToUpdate.Address = model.Address;

        if (!string.IsNullOrEmpty(model.Role))
        {
            var currentRoles = await _userManager.GetRolesAsync(userToUpdate);
            var newRole = model.Role;

            if (currentRoles.Count != 1 || !currentRoles.Contains(newRole))
            {
                var removeRolesResult = await _userManager.RemoveFromRolesAsync(userToUpdate, currentRoles);
                if (!removeRolesResult.Succeeded)
                {
                    foreach (var error in removeRolesResult.Errors)
                        ModelState.AddModelError("Role", $"Lỗi xóa vai trò cũ: {error.Description}");
                    return BadRequest(ModelState);
                }

                var addRoleResult = await _userManager.AddToRoleAsync(userToUpdate, newRole);
                if (!addRoleResult.Succeeded)
                {
                    foreach (var error in addRoleResult.Errors)
                        ModelState.AddModelError("Role", $"Lỗi thêm vai trò mới: {error.Description}");
                    return BadRequest(ModelState);
                }
            }
        }
        else
        {
            var currentRoles = await _userManager.GetRolesAsync(userToUpdate);
            if (currentRoles.Any())
            {
                var removeRolesResult = await _userManager.RemoveFromRolesAsync(userToUpdate, currentRoles);
                if (!removeRolesResult.Succeeded)
                {
                    foreach (var error in removeRolesResult.Errors)
                        ModelState.AddModelError("Role", $"Lỗi xóa vai trò khi role trống: {error.Description}");
                    return BadRequest(ModelState);
                }
            }
        }

        if (!string.IsNullOrEmpty(model.Password))
        {
            var removePasswordResult = await _userManager.RemovePasswordAsync(userToUpdate);

            if (!removePasswordResult.Succeeded && !removePasswordResult.Errors.Any(e => e.Code == "PasswordNotFound"))
            {
                foreach (var error in removePasswordResult.Errors)
                {
                    ModelState.AddModelError("Password", $"Lỗi khi xóa mật khẩu cũ: {error.Description}");
                }
                return BadRequest(ModelState);
            }

            var addPasswordResult = await _userManager.AddPasswordAsync(userToUpdate, model.Password);
            if (!addPasswordResult.Succeeded)
            {
                foreach (var error in addPasswordResult.Errors)
                {
                    ModelState.AddModelError("Password", $"Mật khẩu mới không hợp lệ: {error.Description}");
                }
                return BadRequest(ModelState);
            }
        }

        var updateResult = await _userManager.UpdateAsync(userToUpdate);
        if (!updateResult.Succeeded)
        {
            foreach (var error in updateResult.Errors)
                ModelState.AddModelError("", $"Lỗi khi cập nhật thông tin người dùng: {error.Description}");
            return BadRequest(ModelState);
        }

        return NoContent();
    }






    [HttpGet]
    public IActionResult totalByMonth([FromQuery] int year)
    {
        var result = new List<object>();
        try
        {
            if (year == 0)
            {
                year = DateTime.Now.Year;
            }

            var userList = _context.Users.AsQueryable();


            for (int i = 1; i <= 12; i++)
            {

                var endDate = (i < 12) ? new DateTime(year, i + 1, 1) : new DateTime(year + 1, 1, 1);

                result.Add(new
                {
                    Month = i,
                    count = userList.Count(u => u.RegisteredAt < endDate)
                });
            }
        }
        catch (Exception e)
        {
            _logger.LogInformation(e.Message);
        }


        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> CountByRole()
    {
        try
        {
            // 1) Lấy về số lượng user theo RoleId
            var countsByRole = await _context.UserRoles
                .GroupBy(ur => ur.RoleId)
                .Select(g => new
                {
                    RoleId = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            // 2) Join với bảng Roles để có RoleName
            var joined = countsByRole
                .Join(
                    _context.Roles,
                    cr => cr.RoleId,
                    r => r.Id,
                    (cr, r) => new
                    {
                        RoleName = r.Name,
                        Count = cr.Count
                    })
                .ToList();

            // 3) Tách ra 2 mảng: labels (RoleName) và data (Count)
            var labels = joined.Select(x => x.RoleName).ToList();
            var data = joined.Select(x => x.Count).ToList();

            // 4) Trả về JSON
            return Ok(new
            {
                labels,
                data
            });
        }
        catch (Exception ex)
        {
            // LOG exception ở đây nếu cần
            return StatusCode(500, "Có lỗi khi lấy dữ liệu.");
        }
    }

    [HttpGet]
    public IActionResult newByMonth([FromQuery] int year)
    {
        var result = new List<Object>();
        try
        {
            if (year == 0)
            {
                year = DateTime.Now.Year;
            }
            var userEachMonth = _context.Users.Where(u => u.RegisteredAt.Year == year).GroupBy(u => u.RegisteredAt.Month).Select(u => new
            {
                Month = u.Key,
                Count = u.Count()
            }).ToDictionary(u => u.Month, u => u.Count);

            for (int i = 1; i <= 12; i++)
            {
                result.Add(new
                {
                    Month = i,
                    Count = userEachMonth.GetValueOrDefault(i, 0)
                });
            }
        }
        catch (Exception e)
        {

        }


        return Ok(result);
    }
    public IActionResult AverageUsageByDay()
    {
        var today = DateTime.UtcNow.Date;
        var labels = new List<string>();
        var data = new List<int>(); // thời gian tính theo phút

        for (int i = 6; i >= 0; i--)
        {
            var day = today.AddDays(-i);
            var nextDay = day.AddDays(1);

            // Label dạng "Thứ hai", "Thứ ba", ...
            string label = GetVietnameseDayName(day.DayOfWeek);
            labels.Add(label);

            // Lấy tất cả login trong ngày đó
            var histories = _context.LoginHistory
                .Where(h => h.LoginTime >= day && h.LoginTime < nextDay)
                .ToList();

            double totalSeconds = 0;
            foreach (var h in histories)
            {
                var login = h.LoginTime;
                var logout = h.logoutTime > h.LoginTime ? h.logoutTime : h.LoginTime;
                totalSeconds += (logout - login).TotalSeconds;
            }

            data.Add((int)Math.Round(totalSeconds / 60));
        }

        return Ok(new { labels, data });
    }

    private string GetVietnameseDayName(DayOfWeek day)
    {
        return day switch
        {
            DayOfWeek.Monday => "Thứ hai",
            DayOfWeek.Tuesday => "Thứ ba",
            DayOfWeek.Wednesday => "Thứ tư",
            DayOfWeek.Thursday => "Thứ năm",
            DayOfWeek.Friday => "Thứ sáu",
            DayOfWeek.Saturday => "Thứ bảy",
            DayOfWeek.Sunday => "Chủ nhật",
        };
    }

    [HttpGet]
    public async Task<IActionResult> getAvatar()
    {
        string adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(adminUserId))
        {
            return Unauthorized(new { success = false, message = "Người dùng chưa xác thực." });
        }

        // We are fetching avatars relevant to the admin's context for selection
        var adminUser = await _userManager.FindByIdAsync(adminUserId);
        string currentAdminAvatarForDisplay = adminUser?.AvatarUrl; // This is admin's own avatar

        var vm = new AvatarViewModel
        {
            // CurrentAvatar here can be used to display admin's avatar if needed,
            // but for selecting avatar FOR A NEW USER, client-side will initialize its selection.
            CurrentAvatar = currentAdminAvatarForDisplay,
            DefaultAvatars = _avatarService.GetDefaultAvatars(),
            UploadedAvatars = await _avatarService.GetUploadedAvatarsAsync(adminUserId)
        };
        return Json(new { success = true, data = vm });
    }
    //upload avatar
    [HttpPost]
    [ValidateAntiForgeryToken]

    public async Task<IActionResult> UploadAndSelectAvatar(IFormFile avatarFile, string selectedFileName)
    {
        // ... (code lấy adminUserId không đổi)
        string adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(adminUserId))
        {
            return Json(new { success = false, message = "Lỗi xác thực người quản trị." });
        }

        var (success, message, newAvatarPath, processedFileName) =
            await _avatarService.UploadOrSelectAvatarAsync(avatarFile, selectedFileName, adminUserId);

        if (!success)
        {
            return Json(new { success = false, message });
        }


        string webSafeAvatarPath = newAvatarPath?.Replace("\\", "/");

        _logger.LogInformation("Admin {AdminUserId} chuẩn bị avatar '{FileName}', đường dẫn đã chuẩn hóa: {Path}", adminUserId, processedFileName, webSafeAvatarPath);

        return Json(new { success = true, message, newAvatarPath = webSafeAvatarPath });
    }
    // Đặt bên trong class UserController
    [HttpPost]
    [ValidateAntiForgeryToken]

    public async Task<IActionResult> UploadSelectAndUpdateUserAvatar(string currentUserID, IFormFile avatarFile, string selectedFileName)
    {
       
        string adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(adminUserId))
        {
            return Json(new { success = false, message = "Lỗi xác thực người quản trị." });
        }

        var (success, message, newAvatarPath, processedFileName) =
            await _avatarService.UploadOrSelectAvatarAsync(avatarFile, selectedFileName, adminUserId);

        if (!success)
        {
            return Json(new { success = false, message });
        }


        string webSafeAvatarPath = newAvatarPath?.Replace("\\", "/");
        var currentUser = await _userManager.FindByIdAsync(currentUserID);
        if (currentUser == null) {
            return BadRequest( message="ko tìm thấy người  dùng");
        }
        if (selectedFileName != null)
        {
            currentUser.AvatarUrl = webSafeAvatarPath;
            var updateResult = await _userManager.UpdateAsync(currentUser);
            if (!updateResult.Succeeded)
            {
                return Json(new { success = false, message = "Lỗi khi cập nhật avatar cho người dùng: " + string.Join(", ", updateResult.Errors.Select(e => e.Description)) });

            }
        }
        

        return Json(new { success = true, message, newAvatarPath = webSafeAvatarPath });
    }



    [HttpGet]
    public IActionResult CheckAdminRecentAuth()
    {
        _logger.LogInformation("Action CheckAdminRecentAuth đã được gọi. User authenticated: {IsAuth}", User.Identity.IsAuthenticated);

        const int REAUTH_TIMEOUT_MINUTES = 20;
        var adminLastAuthTimeStr = HttpContext.Session.GetString("AdminLastAuthTimeUtc");

        _logger.LogInformation("Giá trị AdminLastAuthTimeUtc từ session trong CheckAdminRecentAuth: '{SessionValue}'", adminLastAuthTimeStr);

        if (HttpContext.Session == null)
        {
            _logger.LogError("HttpContext.Session là NULL trong CheckAdminRecentAuth. Cấu hình Session có thể có vấn đề.");
            return Json(new { recentlyAuthenticated = false });
        }

        if (!string.IsNullOrEmpty(adminLastAuthTimeStr) &&
            DateTime.TryParse(adminLastAuthTimeStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime lastAuthTimeUtc) &&
            (DateTime.UtcNow - lastAuthTimeUtc) < TimeSpan.FromMinutes(REAUTH_TIMEOUT_MINUTES))
        {
            _logger.LogInformation("Admin đã xác thực gần đây (trong CheckAdminRecentAuth): true");
            return Json(new { recentlyAuthenticated = true });
        }

        _logger.LogInformation("Admin đã xác thực gần đây (trong CheckAdminRecentAuth): false");
        return Json(new { recentlyAuthenticated = false });
    }

    // Đặt hàm này vào bên trong class UserController của bạn

    [HttpPost]
   
    public async Task<IActionResult> BulkLockUsers([FromBody] List<string> userIds)
    {
        if (userIds == null || !userIds.Any())
        {
            return BadRequest(new { success = false, message = "Không có người dùng nào được chọn để khóa." });
        }

        var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(adminUserId))
        {
            return Unauthorized(new { success = false, message = "Không thể xác thực quản trị viên." });
        }

        int successCount = 0;
        var errorMessages = new List<string>();

        foreach (var userId in userIds)
        {
            // Ngăn không cho admin tự khóa chính mình
            if (userId == adminUserId)
            {
                var adminEmail = (await _userManager.FindByIdAsync(userId))?.Email;
                errorMessages.Add($"Bạn không thể tự khóa tài khoản của chính mình ({adminEmail}).");
                continue;
            }

            var userToLock = await _userManager.FindByIdAsync(userId);
            if (userToLock == null)
            {
                errorMessages.Add($"Không tìm thấy người dùng với ID: {userId}");
                continue;
            }

            try
            {
                // === SAO CHÉP LOGIC KHÓA VĨNH VIỄN TỪ LockToggle ===
                // 1. Bật tính năng Lockout cho tài khoản
                var enableLockoutResult = await _userManager.SetLockoutEnabledAsync(userToLock, true);
                if (!enableLockoutResult.Succeeded)
                {
                    var errors = string.Join(", ", enableLockoutResult.Errors.Select(e => e.Description));
                    errorMessages.Add($"Lỗi khi bật chế độ khóa cho {userToLock.Email}: {errors}");
                    continue; // Bỏ qua người dùng này
                }

                // 2. Đặt ngày hết hạn khóa về giá trị lớn nhất (vĩnh viễn)
                var lockResult = await _userManager.SetLockoutEndDateAsync(userToLock, DateTimeOffset.MaxValue);
                // === KẾT THÚC LOGIC SAO CHÉP ===

                if (lockResult.Succeeded)
                {
                    successCount++;
                    _logger.LogInformation("Đã khóa thành công tài khoản {UserId} bởi quản trị viên {AdminId}", userId, adminUserId);
                }
                else
                {
                    var errors = string.Join(", ", lockResult.Errors.Select(e => e.Description));
                    errorMessages.Add($"Lỗi khi khóa tài khoản {userToLock.Email}: {errors}");
                    _logger.LogError("Lỗi khi khóa tài khoản {UserId}: {Errors}", userId, errors);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi hệ thống khi khóa tài khoản {UserId}", userId);
                errorMessages.Add($"Lỗi hệ thống khi khóa tài khoản {userToLock.Email}.");
            }
        }

        // Xây dựng thông báo trả về (giữ nguyên logic như cũ)
        if (successCount == userIds.Count)
        {
            return Ok(new { success = true, message = $"Đã khóa thành công {successCount} tài khoản." });
        }

        if (successCount > 0)
        {
            string finalMessage = $"Đã khóa thành công {successCount} tài khoản. Tuy nhiên, có {errorMessages.Count} lỗi xảy ra: {string.Join("; ", errorMessages)}";
            return Ok(new { success = true, message = finalMessage });
        }

        return BadRequest(new { success = false, message = $"Không thể khóa bất kỳ tài khoản nào. Lỗi: {string.Join("; ", errorMessages)}" });
    }




}
