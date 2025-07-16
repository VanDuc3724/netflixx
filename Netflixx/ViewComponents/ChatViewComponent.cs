using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Netflixx.Models; // Đảm bảo bạn có using đến model của User
using System.Security.Claims;
using System.Threading.Tasks;

namespace Netflixx.ViewComponents
{
    public class ChatViewComponent : ViewComponent
    {
        private readonly UserManager<AppUserModel> _userManager;

        // Dùng dependency injection để lấy UserManager
        public ChatViewComponent(UserManager<AppUserModel> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Kiểm tra xem người dùng đã đăng nhập chưa
            if (User.Identity.IsAuthenticated)
            {
                // Lấy thông tin người dùng hiện tại
                var user = await _userManager.GetUserAsync((ClaimsPrincipal)User);

                if (user != null)
                {
                    // Tạo một ViewModel nhỏ chỉ dành cho chat
                    var viewModel = new UserViewModel // Bạn có thể tái sử dụng UserViewModel ở đây
                    {
                        Id = user.Id,
                        Name = user.UserName, // Giả sử tên thuộc tính là UserName
                        AvatarUrl = user.AvatarUrl, // Giả sử tên thuộc tính là Avatar,
                        Role=   "User" // Giả sử bạn có thuộc tính Role trong AppUserModel
                    };

                    // Trả về một Partial View và truyền model vào
                    return View(viewModel);
                }
            }

            // Nếu người dùng chưa đăng nhập, không render ra gì cả
            return Content(string.Empty);
        }
    }
}