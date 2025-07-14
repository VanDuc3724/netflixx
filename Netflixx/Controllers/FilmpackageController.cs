using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Netflixx.Models;
using Netflixx.Repositories;

namespace Netflixx.Controllers
{
    [Authorize]
    public class FilmpackageController : Controller
    {
        private readonly UserManager<AppUserModel> _userManager;
        private readonly DBContext _context;

        public FilmpackageController(UserManager<AppUserModel> userManager, DBContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Billhistory()
        {
            var user = await _userManager.GetUserAsync(User);
            var history = await _context.PointsTransactions
                .Where(p => p.UserID == user.Id)
                .OrderByDescending(p => p.TransactionDate)
                .ToListAsync();
            return View(history);
        }

        [HttpGet]
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Buy(FilmPackageViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            var account = await _context.UserAccounts.FirstOrDefaultAsync(a => a.UserID == user.Id);

            if (account == null || account.PointsBalance < model.PackagePrice)
            {
                ModelState.AddModelError(string.Empty, "Không đủ coin để thanh toán.");
                return View(model);
            }

            account.PointsBalance -= model.PackagePrice;

            _context.PointsTransactions.Add(new PointsTransactionsModel
            {
                UserID = user.Id,
                TransactionDate = DateTime.UtcNow,
                PointsChange = -model.PackagePrice,
                Reason = $"Mua gói {model.PackageName}"
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Thanh toán thành công";
            return RedirectToAction(nameof(Billhistory));
        }

    }
}
