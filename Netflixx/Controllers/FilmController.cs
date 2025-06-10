using Microsoft.AspNetCore.Mvc;
using Netflixx.Models;
using Netflixx.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Netflixx.Controllers
{
    public class FilmController : Controller
    {
        private readonly DBContext _db;

        public FilmController(DBContext db)
        {
            _db = db;
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
                // Get newest films (5 films) ordered by ReleaseDate
                var newestFilms = await filmsQuery
                    .OrderByDescending(f => f.ReleaseDate)
                    .Take(5)
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


        public async Task<IActionResult> Detail(int id)
        {
            var film = await _db.Films.FirstOrDefaultAsync(f => f.Id == id);
            if (film == null)
            {
                return NotFound();
            }
            return View(film);
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
    }
}