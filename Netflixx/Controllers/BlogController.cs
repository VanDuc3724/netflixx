using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Netflixx.Models;
using Netflixx.Repositories;
using System;
using System.Linq;
using System.Threading.Tasks;

public class BlogController : Controller
{
    private readonly DBContext _context;

    public BlogController(DBContext context)
    {
        _context = context;
    }

    // GET: Blog - Updated to include genre filtering
    public async Task<IActionResult> Index(string sortBy = "date", string sortOrder = "desc", string genreFilter = "")
    {
        var blogPostsQuery = _context.BlogPosts
            .Include(b => b.Film)
            .AsQueryable();

        // Apply genre filter if specified
        if (!string.IsNullOrEmpty(genreFilter))
        {
            blogPostsQuery = blogPostsQuery.Where(b => b.Film != null && b.Film.Genre == genreFilter);
        }

        // Apply sorting
        blogPostsQuery = ApplySorting(blogPostsQuery, sortBy, sortOrder);

        // Get unique genres for filter dropdown
        ViewBag.Genres = await _context.Films
            .Where(f => !string.IsNullOrEmpty(f.Genre))
            .Select(f => f.Genre)
            .Distinct()
            .OrderBy(g => g)
            .ToListAsync();

        // Pass current filter and sorting parameters to view
        ViewBag.CurrentSort = sortBy;
        ViewBag.CurrentOrder = sortOrder;
        ViewBag.CurrentGenreFilter = genreFilter;
        ViewBag.DateSortParam = sortBy == "date" ? (sortOrder == "asc" ? "desc" : "asc") : "asc";
        ViewBag.GenreSortParam = sortBy == "genre" ? (sortOrder == "asc" ? "desc" : "asc") : "asc";

        return View(await blogPostsQuery.ToListAsync());
    }

    // GET: Blog/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var blogPost = await _context.BlogPosts
            .Include(b => b.Film)
            .FirstOrDefaultAsync(m => m.Id == id);

        return blogPost == null ? NotFound() : View(blogPost);
    }

    // GET: Blog/Create
    public IActionResult Create()
    {
        ViewBag.Films = _context.Films.ToList();
        return View();
    }

    // POST: Blog/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Id,Title,Content,FilmId")] BlogPost blogPost)
    {
        if (ModelState.IsValid)
        {
            blogPost.CreatedDate = DateTime.Now;
            _context.Add(blogPost);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Films = _context.Films.ToList();
        return View(blogPost);
    }

    // GET: Blog/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var blogPost = await _context.BlogPosts.FindAsync(id);
        if (blogPost == null) return NotFound();

        ViewBag.Films = _context.Films.ToList();
        return View(blogPost);
    }

    // POST: Blog/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Content,FilmId,CreatedDate")] BlogPost blogPost)
    {
        if (id != blogPost.Id) return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                blogPost.LastUpdated = DateTime.Now;
                _context.Update(blogPost);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BlogPostExists(blogPost.Id))
                    return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Films = _context.Films.ToList();
        return View(blogPost);
    }

    // GET: Blog/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var blogPost = await _context.BlogPosts
            .Include(b => b.Film)
            .FirstOrDefaultAsync(m => m.Id == id);

        return blogPost == null ? NotFound() : View(blogPost);
    }

    // POST: Blog/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var blogPost = await _context.BlogPosts.FindAsync(id);
        _context.BlogPosts.Remove(blogPost);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // Additional API endpoint for AJAX filtering (optional)
    [HttpGet]
    public async Task<IActionResult> GetGenres()
    {
        var genres = await _context.Films
            .Where(f => !string.IsNullOrEmpty(f.Genre))
            .Select(f => f.Genre)
            .Distinct()
            .OrderBy(g => g)
            .ToListAsync();

        return Json(genres);
    }

    // Helper method to get blog posts count by genre (for analytics)
    [HttpGet]
    public async Task<IActionResult> GetBlogPostStats()
    {
        var stats = await _context.BlogPosts
            .Include(b => b.Film)
            .Where(b => b.Film != null)
            .GroupBy(b => b.Film.Genre)
            .Select(g => new { Genre = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync();

        return Json(stats);
    }

    private bool BlogPostExists(int id)
    {
        return _context.BlogPosts.Any(e => e.Id == id);
    }

    private IQueryable<BlogPost> ApplySorting(IQueryable<BlogPost> query, string sortBy, string sortOrder)
    {
        return sortBy.ToLower() switch
        {
            "genre" => sortOrder == "asc"
                ? query.OrderBy(b => b.Film != null ? b.Film.Genre : "")
                      .ThenByDescending(b => b.CreatedDate)
                : query.OrderByDescending(b => b.Film != null ? b.Film.Genre : "")
                       .ThenByDescending(b => b.CreatedDate),

            "date" => sortOrder == "asc"
                ? query.OrderBy(b => b.CreatedDate)
                : query.OrderByDescending(b => b.CreatedDate),

            _ => query.OrderByDescending(b => b.CreatedDate) // Default sort
        };
    }

    // You can remove this method since we've integrated the functionality into the main Index method
    // public async Task<IActionResult> IndexWithFilter(string sortBy = "date", string sortOrder = "desc", string genreFilter = "")
    // {
    //     // This functionality is now in the main Index method
    // }
}