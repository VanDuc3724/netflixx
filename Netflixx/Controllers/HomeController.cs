using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Netflixx.Models;
using Netflixx.Models.ViewModel;
using Netflixx.Repositories;

namespace Netflixx.Controllers
{
    public class HomeController : Controller
    {
        private readonly DBContext _db;
        private readonly UserManager<AppUserModel> _userManager;
        private readonly ILogger<HomeController> _logger;

        public HomeController(DBContext db, UserManager<AppUserModel> userManager, ILogger<HomeController> logger)
        {
            _db = db;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                user = new AppUserModel
                {
                    FullName = "Sample User",
                    Email = "sample@example.com",
                    PhoneNumber = "000-000-0000",
                };
            }

            // pull 7 random films for carousel
            var randomFilms = await _db.Films
                                       .OrderBy(f => Guid.NewGuid())
                                       .Take(7)
                                       .ToListAsync();
            var cheapest = await _db.Films
                            .Where(f => f.Price.HasValue)
                            .OrderBy(f => f.Price.Value)
                            .FirstOrDefaultAsync();

            var vm = new HomeViewModel
            {
                CurrentUser = user,
                CarouselFilms = randomFilms,
                RecommendedFilms = randomFilms,      // you can keep separate later
                TopRatedFilms = await _db.Films
                                             .OrderByDescending(f => f.Rating)
                                             .ThenByDescending(f => f.ReleaseDate)
                                             .Take(7)
                                             .ToListAsync(),

                CheapestFilm = cheapest
            };

            ViewBag.AvatarUrl = user.AvatarUrl;

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Profile(bool edit = false)
        {



            var user = await _userManager.GetUserAsync(User);

            if (!(user == null))
                _logger.LogInformation("CurrentUser session JSON: {SessionRaw}", user);
            else
                _logger.LogInformation("No CurrentUser found in Session");


            // if nobody's signed in, use a sample user for testing
            if (user == null)
            {
                user = new AppUserModel
                {
                    FullName = "Sample User",
                    Email = "sample@example.com",
                    PhoneNumber = "000-000-0000",
                    DateOfBirth = DateTime.Today.AddYears(-30),
                    Address = "1234 Example St.",
                    AvatarUrl = "/image/logo/avatar_default.jpg",
                    FavoriteGenres = "Action,Drama,Documentary"
                };
            }

            // map to your input view‐model
            var input = new ProfileInputModel
            {
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                DateOfBirth = user.DateOfBirth,
                Address = user.Address,
                AvatarUrl = user.AvatarUrl,
                Genres = string.IsNullOrWhiteSpace(user.FavoriteGenres)
                                 ? new List<string>()
                                 : user.FavoriteGenres.Split(',').ToList(),
                About = user.About,

            };

            ViewBag.AvatarUrl = user.AvatarUrl;

            ViewData["EditMode"] = edit;
            return View(input);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(ProfileInputModel input)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            if (!string.IsNullOrWhiteSpace(input.PhoneNumber))
            {
                var conflict = await _userManager.Users
                    .AnyAsync(u => u.PhoneNumber == input.PhoneNumber && u.Id != user.Id);
                if (conflict)
                    ModelState.AddModelError(
                        nameof(input.PhoneNumber),
                        "This phone number is already in use by another account."
                    );
            }

            // simple validation
            if (!ModelState.IsValid)
            {
                // Log each validation error
                foreach (var kv in ModelState)
                {
                    foreach (var err in kv.Value.Errors)
                    {
                        _logger.LogWarning("ModelState error for '{Field}': {Error}", kv.Key, err.ErrorMessage);
                    }
                }
                ViewData["EditMode"] = true;
                return View("Profile", input);
            }


            // update fields
            user.FullName = input.FullName;
            user.Email = user.Email;
            user.PhoneNumber = input.PhoneNumber;
            user.DateOfBirth = input.DateOfBirth;
            user.Address = input.Address;
            user.About = input.About;        // ← save the textarea
            // genres -> comma string
            user.FavoriteGenres = string.Join(",", input.Genres.Where(g => !string.IsNullOrWhiteSpace(g)));

            // avatar upload
            if (input.AvatarFile != null)
            {
                var ext = Path.GetExtension(input.AvatarFile.FileName);
                var fn = $"{user.Id}{ext}";
                var savePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "image", "avatars", fn);
                using var fs = new FileStream(savePath, FileMode.Create);
                await input.AvatarFile.CopyToAsync(fs);
                user.AvatarUrl = $"/image/avatars/{fn}";
            }

            await _userManager.UpdateAsync(user);
            return RedirectToAction(nameof(Profile));
        }

        [HttpGet]
        public async Task<IActionResult> History(string period = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction(nameof(Index));

            // pull all purchases for this user
            var purchases = await _db.FilmPurchases
                                     .Include(fp => fp.Film)
                                     .Where(fp => fp.UserID == user.Id)
                                     .ToListAsync();

            // make a list of "YYYY-MM"
            var periods = purchases
                .Select(fp => fp.PurchaseDate.ToString("yyyy-MM"))
                .Distinct()
                .OrderByDescending(p => p)
                .ToList();

            // default to the most recent period
            if (string.IsNullOrEmpty(period) && periods.Any())
                period = periods.First();

            // filter for that period
            var filmsThis = purchases
                .Where(fp => fp.PurchaseDate.ToString("yyyy-MM") == period)
                .Select(fp => fp.Film)
                .ToList();

            // pick 5 random films as “recommended”
            var recs = await _db.Films
                               .OrderBy(f => f.Id)
                               .Take(4)
                               .ToListAsync();

            var vm = new HistoryViewModel
            {
                RegisteredAt = user.RegisteredAt,
                AvailablePeriods = periods,
                SelectedPeriod = period,
                FilmsThisPeriod = filmsThis,
                RecommendedFilms = recs
            };

            ViewBag.AvatarUrl = user.AvatarUrl;

            return View(vm);
        }
    }
}
