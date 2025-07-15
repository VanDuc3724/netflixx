using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Netflixx.Models;
using Netflixx.Models.ViewModel;
using Netflixx.Repositories;

namespace Netflixx.Controllers
{
    [Authorize]
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

        public async Task<IActionResult> Billhistory()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var transactions = await _context.PaymentTransactions
                .Include(t => t.Provider)
                .Include(t => t.Environment)
                .Where(t => t.UserID == user.Id)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();

            var currentPackage = await _context.PackageSubscriptions
                .Include(p => p.Package)
                .Where(p => p.UserID == user.Id && p.EndDate >= DateTime.UtcNow)
                .OrderByDescending(p => p.StartDate)
                .FirstOrDefaultAsync();

            var vm = new BillHistoryViewModel
            {
                CurrentPackage = currentPackage,
                Transactions = transactions
            };

            return View(vm);
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
