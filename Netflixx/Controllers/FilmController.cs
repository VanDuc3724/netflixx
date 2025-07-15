using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Netflixx.Models;
using Netflixx.Models.ViewModel;
using Netflixx.Repositories;
using System.IO;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks;

namespace Netflixx.Controllers
{
    public class FilmController : Controller
    {
        private readonly DBContext _db;
        private readonly UserManager<AppUserModel> _userManager;
        private readonly IRazorViewEngine _viewEngine;

        public FilmController(DBContext db, UserManager<AppUserModel> userManager, IRazorViewEngine viewEngine)
        {
            _db = db;
            _userManager = userManager;
            _viewEngine = viewEngine;
        }

        public async Task<IActionResult> Index(string searchString, string genreFilter, int? yearFilter)
        {
            var filmsQuery = _db.Films.AsQueryable();

            // Filter by search string
            if (!string.IsNullOrEmpty(searchString))
            {
                filmsQuery = filmsQuery.Where(f => f.Title.Contains(searchString) ||
                             (f.Description != null && f.Description.Contains(searchString)));
            }

            // Filter by genre
            if (!string.IsNullOrEmpty(genreFilter))
            {
                filmsQuery = filmsQuery.Where(f => f.Genre == genreFilter);
            }

            // Filter by year
            if (yearFilter.HasValue)
            {
                filmsQuery = filmsQuery.Where(f => f.ReleaseDate.HasValue &&
                                                  f.ReleaseDate.Value.Year == yearFilter.Value);
            }

            // Get distinct genres for filter dropdown
            var genres = await _db.Films
                .Select(f => f.Genre)
                .Distinct()
                .ToListAsync();

            // Get distinct years for filter dropdown
            var years = await _db.Films
                .Where(f => f.ReleaseDate.HasValue)
                .Select(f => f.ReleaseDate.Value.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToListAsync();

            var films = await filmsQuery.ToListAsync();

            ViewBag.Genres = genres;
            ViewBag.Years = years;
            ViewBag.SearchString = searchString;
            ViewBag.SelectedGenre = genreFilter;
            ViewBag.SelectedYear = yearFilter;

            return View(films);
        }

        public async Task<IActionResult> Type(string genreFilter)
        {
            var filmsQuery = _db.Films.AsQueryable();

            // Filter by genre if specified and not "all"
            if (!string.IsNullOrEmpty(genreFilter) && genreFilter != "all")
            {
                filmsQuery = filmsQuery.Where(f => f.Genre == genreFilter);
            }

            // Get distinct genres for filter buttons
            var genres = await _db.Films
                .Select(f => f.Genre)
                .Distinct()
                .ToListAsync();

            List<FilmsModel> films;

            if (string.IsNullOrEmpty(genreFilter))
            {
                // Get newest films (3 films) ordered by ReleaseDate
                var newestFilms = await filmsQuery
                    .OrderByDescending(f => f.ReleaseDate)
                    .Take(3)
                    .ToListAsync();

                // Store in ViewBag to access separately in view
                ViewBag.FeaturedFilm = newestFilms.FirstOrDefault(); // Phim nổi bật
                ViewBag.OtherNewFilms = newestFilms.Skip(1).Take(4).ToList(); // 4 phim còn lại

                films = newestFilms;
            }
            else if (genreFilter == "all")
            {
                // Show all films ordered by newest first
                films = await filmsQuery
                    .OrderByDescending(f => f.ReleaseDate)
                    .ToListAsync();
            }
            else
            {
                // Show films by genre
                films = await filmsQuery
                    .OrderByDescending(f => f.ReleaseDate)
                    .ToListAsync();
            }

            ViewBag.Genres = genres;
            ViewBag.SelectedGenre = genreFilter;

            return View(films);
        }


        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {

            var user = await _userManager.GetUserAsync(User);




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

            var film = await _db.Films
                                .Include(f => f.Purchases)
                                .FirstOrDefaultAsync(f => f.Id == id);
            if (film == null) return NotFound();

            var comments = await _db.Set<FilmComment>()
                                    .Where(c => c.FilmId == id)
                                    .OrderBy(c => c.Level)
                                    .ThenBy(c => c.CreatedAt)
                                    .ToListAsync();

            var recent = await _db.Films
                                  .OrderByDescending(f => f.ReleaseDate)
                                  .Take(5)
                                  .ToListAsync();

            // pull all 1–10 ratings for this film
            var allRatings = await _db.FilmRatings
                                      .Where(r => r.FilmId == id)
                                      .ToListAsync();

            // raw average on 1–10
            var rawAvg = allRatings.Any()
                ? allRatings.Average(r => r.RatingValue)
                : 0.0;

            // your user’s own rating
            int userRating = 0;
            if (User.Identity.IsAuthenticated)
            {
                var u = await _userManager.GetUserAsync(User);
                userRating = allRatings
                    .FirstOrDefault(r => r.UserId == u.Id)?
                    .RatingValue ?? 0;
            }

            var activeTab = Request.Query["tab"].ToString() ?? "info";

            var vm = new FilmDetailViewModel
            {
                Film = film,
                Comments = comments,
                RecentFilms = recent,
                ActiveTab = activeTab,
                AverageRating = rawAvg,
                RatingCount = allRatings.Count,
                UserRating = userRating
            };

            ViewBag.AvatarUrl = user.AvatarUrl;

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> PostComment(FilmDetailViewModel vm)
        {
            var user = await _userManager.GetUserAsync(User);

            if (string.IsNullOrWhiteSpace(vm.NewCommentContent))
                return RedirectToAction(nameof(Detail), new { id = vm.Film.Id });

            var comment = new FilmComment
            {
                FilmId = vm.Film.Id,
                AuthorName = user != null ? user.ToString() : "Anonymous",
                Content = vm.NewCommentContent,
                Level = vm.ReplyToCommentId.HasValue ? 2 : 1,
                ParentCommentId = vm.ReplyToCommentId
            };
            _db.Add(comment);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Detail), new { id = vm.Film.Id });
        }

        public async Task<IActionResult> DetailSreach(int id)
        {
            var film = await _db.Films.FirstOrDefaultAsync(f => f.Id == id);
            if (film == null)
            {
                return NotFound();
            }
            return View(film);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> SubmitRating(int FilmId, int RatingValue)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // add or update
            var existing = await _db.FilmRatings
                .FirstOrDefaultAsync(r => r.FilmId == FilmId && r.UserId == user.Id);

            if (existing != null)
            {
                existing.RatingValue = RatingValue;
                existing.RatedAt = DateTime.UtcNow;
            }
            else
            {
                _db.FilmRatings.Add(new FilmRating
                {
                    FilmId = FilmId,
                    UserId = user.Id,
                    RatingValue = RatingValue,
                    RatedAt = DateTime.UtcNow
                });
            }
            await _db.SaveChangesAsync();

            // recompute
            var allRatings = await _db.FilmRatings
                .Where(r => r.FilmId == FilmId)
                .ToListAsync();

            var rawAvg = allRatings.Any()
                ? allRatings.Average(r => r.RatingValue)
                : 0.0;
            var ratingCount = allRatings.Count;
            var userRating2 = existing != null
                ? existing.RatingValue
                : RatingValue;

            // if AJAX, render just the form partial and return JSON
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var vm = new FilmDetailViewModel
                {
                    FilmId = FilmId,
                    AverageRating = rawAvg,
                    RatingCount = ratingCount,
                    UserRating = userRating2
                };

                return Json(new
                {
                    success = true,
                    filmId = FilmId,
                    averageRating = rawAvg,
                    ratingCount = ratingCount,

                });
            }

            // fallback for non-AJAX
            return RedirectToAction(nameof(Detail), new { id = FilmId, tab = "info" });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Purchase(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var film = await _db.Films.FindAsync(id);
            if (film == null) return NotFound();

            var account = await _db.UserAccounts.FirstOrDefaultAsync(a => a.UserID == user.Id);
            var priceCoins = film.Price.HasValue ? (int)Math.Ceiling(film.Price.Value) : 0;
            if (account == null || film.Price == null || account.PointsBalance < priceCoins)
            {
                TempData["error"] = "Not enough coins";
                return RedirectToAction(nameof(Detail), new { id });
            }

            var existing = await _db.FilmPurchases.FirstOrDefaultAsync(p => p.UserID == user.Id && p.FilmID == id);
            if (existing != null)
            {
                return RedirectToAction(nameof(Watch), new { filmId = id });
            }

            account.PointsBalance -= priceCoins;

            var purchase = new FilmPurchasesModel
            {
                UserID = user.Id,
                FilmID = id,
                PointsUsed = priceCoins,
                PricePaid = film.Price.Value,
                PurchaseDate = DateTime.UtcNow
            };

            _db.FilmPurchases.Add(purchase);
            _db.PointsTransactions.Add(new PointsTransactionsModel
            {
                UserID = user.Id,
                TransactionDate = DateTime.UtcNow,
                PointsChange = -priceCoins,
                Reason = $"Purchase film {film.Title}"
            });

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Watch), new { filmId = id });
        }

        [HttpGet]
        public async Task<IActionResult> Watch(int filmId, int episode = 1)
        {

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // 1) Load the film
            var film = await _db.Films.FindAsync(filmId);
            if (film == null) return NotFound();

            var purchased = await _db.FilmPurchases
                .AnyAsync(p => p.UserID == user.Id && p.FilmID == filmId);
            if (!purchased)
            {
                TempData["error"] = "You need to purchase this film.";
                return RedirectToAction(nameof(Detail), new { id = filmId });
            }

            // 2) Load comments and recent films (reuse GET Details logic)
            var comments = await _db.FilmComments
                                    .Where(c => c.FilmId == filmId)
                                    .OrderBy(c => c.Level)
                                    .ThenBy(c => c.CreatedAt)
                                    .ToListAsync();
            var recent = await _db.Films
                                  .OrderByDescending(f => f.ReleaseDate)
                                  .Take(5)
                                  .ToListAsync();

            var allRatings = await _db.FilmRatings.Where(r => r.FilmId == filmId).ToListAsync();
            double rawAvg = allRatings.Any() ? allRatings.Average(r => r.RatingValue) : 0;

            // 4) Sample episode sources (replace with real URLs/paths)
            var episodeSources = new List<string>
        {
            "/image/trailer/insideout1.mp4",
            "/image/trailer/insideout2.mp4",
        };

            // 5) Optional quality variants for the current episode
            var qualitySources = new Dictionary<string, string>
            {
                ["360p"] = episodeSources[episode - 1].Replace(".mp4", "-360p.mp4"),
                ["720p"] = episodeSources[episode - 1].Replace(".mp4", "-720p.mp4"),
                ["1080p"] = episodeSources[episode - 1].Replace(".mp4", "-1080p.mp4")
            };

            // 6) Build ViewModel
            var vm = new FilmWatchViewModel
            {
                Film = film,
                Comments = comments,
                RecentFilms = recent,
                AverageRating = rawAvg,
                RatingCount = allRatings.Count,
                EpisodeSources = episodeSources,
                CurrentEpisode = episode,
                QualitySources = qualitySources
            };

            ViewBag.AvatarUrl = user.AvatarUrl;

            return View("WatchScreen", vm);
        }
    }
}