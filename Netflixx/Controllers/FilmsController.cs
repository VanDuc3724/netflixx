// Controllers/FilmsController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Netflixx.Repositories;

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

        // GET: Films/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var filmsModel = await _context.Films
                .FirstOrDefaultAsync(m => m.Id == id);

            if (filmsModel == null)
            {
                return NotFound();
            }

            return View(filmsModel);
        }
    }
}