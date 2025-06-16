using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;

//[Authorize(Roles = "Admin")]
[Area("Admin")]
public class RolesController : Controller
{

    private readonly RoleManager<IdentityRole> _roleManager;
    public RolesController(RoleManager<IdentityRole> roleManager)
    {
        _roleManager = roleManager;
    }


    // GET: /Roles
    public async Task<IActionResult> Index(string sortType, string search_name)
    {
        var Roles = await _roleManager.Roles.ToListAsync();
        if (!string.IsNullOrEmpty(search_name))
        {
            Roles = Roles.Where(r => r.Name.Contains(search_name, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        if (sortType == "name_asc")
        {
            Roles = Roles.OrderBy(r => r.Name).ToList();
        }
        else if (sortType == "name_desc")
        {
            Roles = Roles.OrderByDescending(r => r.Name).ToList();
        }
        return View(Roles);
    }
    //Get: /Roles/Create
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        return View();
    }

    // POST: /Roles/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName) || await _roleManager.RoleExistsAsync(roleName))
        {
            ModelState.AddModelError("", "Invalid or non-existent Role! ");
            return View("Create");
        }

        var result = await _roleManager.CreateAsync(new IdentityRole(roleName));
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
            return View("Create", await _roleManager.Roles.ToListAsync());
        }

        return RedirectToAction(nameof(Index));
    }

    // POST: /Roles/Delete
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string roleId, string search_name, string SortType)
    {
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role == null)
        {
            return NotFound();
        }

        var result = await _roleManager.DeleteAsync(role);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
            return View("Index", await _roleManager.Roles.ToListAsync());
        }

        return RedirectToAction("Index", new { SortType = SortType, search_name = search_name });
    }

    // GET: /Roles/Edit/{id}
    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role == null) return NotFound();
        return View(role);
    }

    // POST: /Roles/Edit/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, string Name)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role == null) return NotFound();

        role.Name = Name;
        var result = await _roleManager.UpdateAsync(role);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
            return View(role);
        }

        return RedirectToAction(nameof(Index));
    }

}