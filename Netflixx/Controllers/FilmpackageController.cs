using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Netflixx.Models;
using Netflixx.Repositories;

namespace Netflixx.Controllers
{
    public class FilmpackageController : Controller
    {
        private readonly DBContext _context;
        private readonly UserManager<AppUserModel> _userManager;

        public FilmpackageController(DBContext context, UserManager<AppUserModel> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        [Authorize]
        public async Task<IActionResult> Billhistory()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Login");
            }

            var transactions = await _context.PaymentTransactions
                .Where(t => t.UserID == user.Id)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();

            return View(transactions);
        }

        public IActionResult Buy(string packageId, string packageName, int packagePrice)
        {
            var model = new FilmPackageViewModel
            {
                PackageId = packageId,
                PackageName = packageName,
                PackagePrice = packagePrice
            };

            return View(model);
        }
    }
}
