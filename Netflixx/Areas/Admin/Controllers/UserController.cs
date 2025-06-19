using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Netflixx.Areas.Admin.Extensions;
using Netflixx.Models;
using Netflixx.Repositories;
using System.Threading.Tasks;
[Area("Admin")]

public class UserController : Controller
{
    private readonly DBContext _context;
    private readonly UserManager<AppUserModel> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<UserController> _logger;
    public UserController(
      ILogger<UserController> logger,
        DBContext context,
        UserManager<AppUserModel> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {

        return View();
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
        var users = _context.Users.AsQueryable();


        if (search != null)
        {
            search = search.ToLower().Trim();

            users = users.Where(u => u.FullName.ToLower().Contains(search) || u.Email.ToLower().Contains(search) || u.UserName.ToLower().Contains(search));
        }

        var totalCount = users.Count();
        var item = users.Skip((page - 1) * pagesize).Take(pagesize).ToList();
        var totalPage = (int)Math.Ceiling((double)totalCount / pagesize);
        var result = new
        {
            TotalCount = totalCount,
            TotalPage = totalPage,
            page = page,
            PageSize = pagesize,
            Items = item.Select(u => new
            {
                u.Id,
                u.FullName,
                u.Email,
                u.UserName,
                IsActive = !(u.LockoutEnabled && u.LockoutEnd > DateTime.UtcNow),
                CreatedAt = u.RegisteredAt.ToString("dd/MM/yyyy"),
                Role = _userManager.GetRolesAsync(u).Result.FirstOrDefault() ?? "Chưa có vai trò"
            })
        };
        return Ok(result);

    }
    [HttpGet]

    public async Task<IActionResult> getUserDetail(string id)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();

        var role = await _userManager.GetRolesAsync(user);
        var userView = new UserViewModel
        {
            Id=user.Id,
            Name = user.FullName,
            Email = user.Email,
            Role = role.FirstOrDefault() ?? "Chưa có vai trò",
        };
        return PartialView("_UserDetail", userView);
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
        await _userManager.AddToRoleAsync(u, model  .Role);

        return RedirectToAction("Detail", new { id = u.Id });
    }
    public IActionResult totalByMonth([FromQuery]int year)
    {
        if (year == 0)
        {
            year = DateTime.Now.Year;
        }
        var total = 0;
        var userList = _context.Users.AsQueryable();
        var result = new List<object>();
        for( int i = 1; i <= 12; i++)
        {
            var date = new DateTime(year, i + 1, 1);
            result.Add(new
            {
                Month=i,
               count= userList.Where(u => u.RegisteredAt < date).Select(u => u.Id).Count()
            });
        }

        return Ok(result);
    }
    [HttpGet]
    public async Task<IActionResult> CountByRole()
    {
        var result = await _context.UserRoles
            .GroupBy(ur => ur.RoleId)
            .Select(g => new
            {
                RoleId = g.Key,
                Count = g.Count()
            })
            .Join(
              _context.Roles,
              rc=> rc.RoleId,
              r=> r.Id,
              (rc,r) => new { 
                RoleName=r.Name,
                Count=rc.Count
              } 

            )
            .ToListAsync();

        return Ok(result);
    }

    public IActionResult newByMonth([FromQuery] int year)
    {

        if (year == 0)
        {
            year = DateTime.Now.Year;
        }
        var userEachMonth = _context.Users.Where(u=> u.RegisteredAt.Year==year).GroupBy(u => u.RegisteredAt.Month).Select(u => new
        {
            Month = u.Key,
            Count = u.Count()
        }).ToDictionary(u => u.Month, u => u.Count);
        var result = new  List<Object>();
        for ( int i = 1; i <= 12; i++)
        {
            result.Add( new
            {
                Month = i,
                Count = userEachMonth.GetValueOrDefault(i,0)
            });
        }
        return Ok(result);
    }
    public IActionResult averageUsageByDay()
    {
        return Ok();
    }
   

}
