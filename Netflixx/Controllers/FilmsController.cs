// Controllers/FilmsController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Netflixx.Repositories;
using Netflixx.Models;
using Netflixx.Models.ViewModel;
using System.Linq;
using System.Threading.Tasks;

namespace Netflixx.Controllers
{
    public class FilmsController : Controller
    {
        private readonly DBContext _context;

        public FilmsController(DBContext context)
        {
            _context = context;
        }

        // GET: Films
        public async Task<IActionResult> Index()
        {
            return View(await _context.Films.ToListAsync());
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var film = await _context.Films
                                .Include(f => f.Purchases)
                                .FirstOrDefaultAsync(f => f.Id == id);
            if (film == null) return NotFound();

            var comments = await _context.Set<FilmComment>()
                                    .Where(c => c.FilmId == id)
                                    .OrderBy(c => c.Level)
                                    .ThenBy(c => c.CreatedAt)
                                    .ToListAsync();

            var recent = await _context.Films
                                  .OrderByDescending(f => f.ReleaseDate)
                                  .Take(5)
                                  .ToListAsync();

            var vm = new FilmDetailViewModel
            {
                Film = film,
                Comments = comments,
                RecentFilms = recent
            };

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> PostComment(FilmDetailViewModel vm)
        {
            if (string.IsNullOrWhiteSpace(vm.NewCommentContent))
                return RedirectToAction(nameof(Details), new { id = vm.Film.Id });

            var comment = new FilmComment
            {
                FilmId = vm.Film.Id,
                AuthorName = vm.NewCommentAuthor ?? "Anonymous",
                Content = vm.NewCommentContent,
                Level = vm.ReplyToCommentId.HasValue ? 2 : 1,
                ParentCommentId = vm.ReplyToCommentId
            };
            _context.Add(comment);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = vm.Film.Id });
        }
    }
}