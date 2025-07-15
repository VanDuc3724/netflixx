using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Netflixx.Models;
using Netflixx.Repositories;

namespace Netflixx.Controllers
{
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

        public IActionResult Billhistory()
        {
            return View();
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

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BuyWithCoins([FromBody] FilmPackageViewModel model)
        {
            if (model == null)
            {
                return BadRequest();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var account = await _context.UserAccounts.FirstOrDefaultAsync(a => a.UserID == user.Id);
            if (account == null || account.PointsBalance < model.PackagePrice)
            {
                return Json(new { success = false, message = "Không đủ coins." });
            }

            account.PointsBalance -= model.PackagePrice;

            var package = await _context.Packages.FirstOrDefaultAsync(p => p.Name == model.PackageName);
            if (package == null)
            {
                package = new PackagesModel { Name = model.PackageName };
                _context.Packages.Add(package);
                await _context.SaveChangesAsync();
            }

            var subscription = new PackageSubscriptionsModel
            {
                UserID = user.Id,
                PackageID = package.Id,
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date.AddMonths(1),
                Price = model.PackagePrice,
                Status = "active"
            };
            _context.PackageSubscriptions.Add(subscription);

            _context.PointsTransactions.Add(new PointsTransactionsModel
            {
                UserID = user.Id,
                TransactionDate = DateTime.UtcNow,
                PointsChange = -model.PackagePrice,
                Reason = $"Buy package {model.PackageName}"
            });

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Thanh toán thành công" });
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var films = await _context.Films
                .OrderBy(f => f.Title)
                .ToListAsync();
            var model = new PackageCreateViewModel
            {
                AvailableFilms = films
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PackageCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AvailableFilms = await _context.Films.OrderBy(f => f.Title).ToListAsync();
                return View(model);
            }

            var package = new PackagesModel
            {
                Name = model.Name,
                Description = model.Description
            };

            _context.Packages.Add(package);
            await _context.SaveChangesAsync();

            if (model.SelectedFilmIds.Any())
            {
                foreach (var filmId in model.SelectedFilmIds)
                {
                    _context.PackageFilms.Add(new PackageFilmsModel
                    {
                        PackageID = package.Id,
                        FilmID = filmId
                    });
                }
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Đã tạo gói phim";
            return RedirectToAction(nameof(Index));
        }

    }
}
