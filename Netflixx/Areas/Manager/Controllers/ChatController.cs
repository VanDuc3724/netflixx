using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Netflixx.Models;

[Authorize]
[Area("Manager")]
public class ChatController : Controller
{
    private readonly UserManager<AppUserModel> _userManager;
    public ChatController(UserManager<AppUserModel> um) => _userManager = um;

    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User)!;
        var roles = await _userManager.GetRolesAsync(user);
        bool isManager = roles.Contains("Manager");

        var vm = new UserViewModel
        {
            Id = user.Id,
            Name = user.UserName!,
            AvatarUrl = user.AvatarUrl ?? "",
            Role = isManager ? "Manager" : "User"
        };

        return View( "ManagerChat" , vm);
    }
}
