using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Netflixx.Models;
using Netflixx.Repositories;

namespace Netflixx.Controllers
{
    public class UserController : Controller
    {
        private readonly DBContext _context;
        private readonly UserManager<AppUserModel> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserController(
            DBContext context,
            UserManager<AppUserModel> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string search_name,
            string role_type,
            string sort_order)
        {
            var users = await _userManager.Users.ToListAsync();

          
            if (!string.IsNullOrEmpty(role_type) && role_type != "Tất cả")
            {
                var filtered = new List<AppUserModel>();
                foreach (var u in users)
                {
                    if (await _userManager.IsInRoleAsync(u, role_type))
                        filtered.Add(u);
                }
                users = filtered;
            }

     
            if (!string.IsNullOrEmpty(search_name))
            {
                users = users
                    .Where(u => !string.IsNullOrEmpty(u.UserName)
                             && u.UserName.Contains(search_name, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

          
            sort_order ??= "asc";
            ViewBag.CurrentSortField = "Name";
            ViewBag.CurrentSortOrder = sort_order;

            users = sort_order == "desc"
                ? users.OrderByDescending(u => u.UserName).ToList()
                : users.OrderBy(u => u.UserName).ToList();

            
            var userList = new List<UserViewModel>();
            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                userList.Add(new UserViewModel
                {
                    Id = u.Id,
                    Name = u.FullName ?? u.UserName,
                    Email = u.Email,
                    Role = roles.FirstOrDefault(),
                    IsActive = !(u.LockoutEnabled && u.LockoutEnd > DateTime.UtcNow)
                });
            }

            ViewBag.SelectedRole = role_type ?? "Tất cả";
            ViewBag.AvailableRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();

            return View(userList);
        }

        [HttpGet]
        public async Task<IActionResult> Detail(string id)
        {
            var u = await _userManager.FindByIdAsync(id);
            if (u == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(u);
            var allRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();

            var model = new UserViewModel
            {
                Id = u.Id,
                Name = u.FullName ?? u.UserName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                dateOfBirth = u.DateOfBirth,
                Role = roles.FirstOrDefault(),
                AvailableRoles = allRoles,
                IsActive = !(u.LockoutEnabled && u.LockoutEnd > DateTime.UtcNow)
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Update(UserViewModel model)
        {
            var u = await _userManager.FindByIdAsync(model.Id);
            if (u == null) return NotFound();

    
            u.FullName = model.Name;
            u.Email = model.Email;
            u.PhoneNumber = model.PhoneNumber;
            u.DateOfBirth = model.dateOfBirth;

        
            if (model.IsActive)
            {

                u.LockoutEnabled = false;
                u.LockoutEnd = null;
            }
            else
            {
                u.LockoutEnabled = true;
                u.LockoutEnd = DateTime.MaxValue;
            }

          
            var updateResult = await _userManager.UpdateAsync(u);
            if (!updateResult.Succeeded)
            {
                foreach (var err in updateResult.Errors)
                    ModelState.AddModelError("", err.Description);

            
                model.AvailableRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
                return View("Detail", model);
            }

     
            var currentRoles = await _userManager.GetRolesAsync(u);
            await _userManager.RemoveFromRolesAsync(u, currentRoles);
            await _userManager.AddToRoleAsync(u, model.Role);

            return RedirectToAction("Detail", new { id = u.Id });
        }
    }
}
