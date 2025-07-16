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

        public async Task<IActionResult> Index()
        {
            var packages = await _context.Packages
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .ToListAsync();

            return View(packages);
        }

        public async Task<IActionResult> Billhistory()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var transactions = await _context.PaymentTransactions
                .Include(t => t.Provider)
                .Include(t => t.Environment)
                .Include(t => t.FilmPurchases)
                    .ThenInclude(fp => fp.Film)
                .Include(t => t.RelatedPointsTransactions)
                .Where(t => t.UserID == user.Id)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();

            var currentPackage = await _context.PackageSubscriptions
                .Include(p => p.Package)
                .Where(p => p.UserID == user.Id && p.EndDate >= DateTime.UtcNow)
                .OrderByDescending(p => p.StartDate)
                .FirstOrDefaultAsync();

            var filmPurchases = await _context.FilmPurchases
                .Include(fp => fp.Film)
                .Where(fp => fp.UserID == user.Id)
                .OrderByDescending(fp => fp.PurchaseDate)
                .ToListAsync();

            var historyItems = transactions
                .Select(t => new BillHistoryItem
                {
                    Date = t.TransactionDate,
                    Description = t.ExternalTransactionRef,
                    Status = t.Status,
                    AmountText = $"{t.Amount.ToString("N0")} {t.Currency}",
                    Provider = t.Provider?.Name ?? string.Empty
                })
                .ToList();

            historyItems.AddRange(filmPurchases.Select(p => new BillHistoryItem
            {
                Date = p.PurchaseDate,
                Description = p.Film?.Title ?? string.Empty,
                Status = "success",
                AmountText = p.PricePaid.ToString("N0"),
                Provider = $"{p.PointsUsed} coins"
            }));

            historyItems = historyItems
                .OrderByDescending(h => h.Date)
                .ToList();

            var vm = new BillHistoryViewModel
            {
                CurrentPackage = currentPackage,
                Transactions = transactions,
                FilmPurchases = filmPurchases,
                History = historyItems
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BuyWithCoins(int packageId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var package = await _context.Packages.FindAsync(packageId);
            if (package == null) return NotFound();

            var account = await _context.UserAccounts.FirstOrDefaultAsync(a => a.UserID == user.Id);
            if (account == null || account.PointsBalance < package.Price)
            {
                TempData["error"] = "Không đủ coins để mua gói.";
                return RedirectToAction(nameof(Buy), new { packageId, packageName = package.Name, packagePrice = package.Price });
            }

            var existing = await _context.PackageSubscriptions
                .FirstOrDefaultAsync(ps => ps.UserID == user.Id && ps.PackageID == packageId && ps.EndDate >= DateTime.UtcNow);
            if (existing != null)
            {
                TempData["error"] = "Bạn đã sở hữu gói này.";
                return RedirectToAction(nameof(Index));
            }

            account.PointsBalance -= package.Price;

            var subscription = new PackageSubscriptionsModel
            {
                UserID = user.Id,
                PackageID = package.Id,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(1),
                Price = package.Price,
                Status = "Active"
            };
            _context.PackageSubscriptions.Add(subscription);

            _context.PointsTransactions.Add(new PointsTransactionsModel
            {
                UserID = user.Id,
                TransactionDate = DateTime.UtcNow,
                PointsChange = -package.Price,
                Reason = $"Purchase package {package.Name}"
            });

            await _context.SaveChangesAsync();

            TempData["success"] = "Mua gói thành công!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Filmpackage/Create
        public async Task<IActionResult> Create()
        {
            var vm = new CreatePackageViewModel
            {
                Package = new PackagesModel(),
                Films = await _context.Films.OrderBy(f => f.Title).ToListAsync()
            };
            return View(vm);
        }

        // POST: Filmpackage/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreatePackageViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Films = await _context.Films.OrderBy(f => f.Title).ToListAsync();
                return View(model);
            }

            _context.Packages.Add(model.Package);
            await _context.SaveChangesAsync();

            if (model.SelectedFilmIds != null && model.SelectedFilmIds.Any())
            {
                foreach (var filmId in model.SelectedFilmIds)
                {
                    _context.PackageFilms.Add(new PackageFilmsModel
                    {
                        PackageID = model.Package.Id,
                        FilmID = filmId
                    });
                }
                await _context.SaveChangesAsync();
            }

            TempData["success"] = "Tạo gói phim thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}
