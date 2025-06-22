using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Text.Json;
using Netflixx.Models;
using Netflixx.Repositories;
using System.Linq;
using System.Threading.Tasks;

namespace Netflixx.Controllers
{
    public class PackagesController : Controller
    {
        private readonly DBContext _context;
        private readonly UserManager<AppUserModel> _userManager;

        public PackagesController(DBContext context, UserManager<AppUserModel> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Packages
        public async Task<IActionResult> Index()
        {
            var packages = await _context.Packages.ToListAsync();
            return View(packages);
        }

        // GET: Packages/List
        [AllowAnonymous]
        public async Task<IActionResult> List()
        {
            var packages = await _context.Packages.ToListAsync();
            return View(packages);
        }

        // POST: Packages/Buy/5
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Buy(int id)
        {
            var package = await _context.Packages.FindAsync(id);
            if (package == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            bool hasActive = await _context.PackageSubscriptions
                .AnyAsync(p => p.UserID == user.Id && p.PackageID == id && p.Status == "active" && p.EndDate > DateTime.UtcNow);
            if (hasActive)
            {
                TempData["error"] = JsonSerializer.Serialize("Bạn đã đăng ký gói này rồi.");
                return RedirectToAction(nameof(List));
            }

            var sub = new PackageSubscriptionsModel
            {
                UserID = user.Id,
                PackageID = id,
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date.AddMonths(1),
                Price = package.Price,
                Status = "active"
            };
            _context.PackageSubscriptions.Add(sub);
            await _context.SaveChangesAsync();

            TempData["success"] = JsonSerializer.Serialize("Đăng ký gói thành công.");
            return RedirectToAction("Profile", "Home");
        }

        // GET: Packages/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Packages/Create
        [HttpPost]
        public async Task<IActionResult> Create(PackagesModel package)
        {
            if (ModelState.IsValid)
            {
                _context.Add(package);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(package);
        }

        // GET: Packages/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var package = await _context.Packages.FindAsync(id);
            if (package == null)
            {
                return NotFound();
            }
            return View(package);
        }

        // POST: Packages/Edit/5
        [HttpPost]
        public async Task<IActionResult> Edit(int id, PackagesModel package)
        {
            if (id != package.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(package);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PackageExists(package.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(package);
        }

        // GET: Packages/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var package = await _context.Packages
                .FirstOrDefaultAsync(m => m.Id == id);
            if (package == null)
            {
                return NotFound();
            }

            return View(package);
        }

        // POST: Packages/Delete/5
        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var package = await _context.Packages.FindAsync(id);
            if (package != null)
            {
                _context.Packages.Remove(package);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool PackageExists(int id)
        {
            return _context.Packages.Any(e => e.Id == id);
        }
    }
}
