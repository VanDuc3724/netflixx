using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Netflixx.Models;
using Netflixx.Repositories;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
//7b1ff5751807e23d2b85cb2fc4b4366db72fcb63

public class BlogController : Controller
{
    private readonly DBContext _context;
    private readonly UserManager<AppUserModel> _userManager;
    private const int PageSize = 10; // Number of posts per page

    public BlogController(DBContext context, UserManager<AppUserModel> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: Blog - Updated to include genre filtering and pagination
    public async Task<IActionResult> Index(string searchTerm = "", string dateFilter = "all", string sortBy = "date_desc", int page = 1, bool myPosts = false, bool showDrafts = false)
    {
        await SetAvatarUrl();
        page = Math.Max(1, page);

        var blogPostsQuery = _context.BlogPosts
            .Include(b => b.Film)
            .Include(b => b.Author)
            .AsQueryable();

        // Filter by current user's posts if myPosts is true
        if (myPosts && User.Identity.IsAuthenticated)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            blogPostsQuery = blogPostsQuery.Where(b => b.AuthorId == currentUserId);
            ViewBag.MyPosts = true; // Thêm flag này để view biết đang ở chế độ xem bài viết cá nhân
        }

        // Filter drafts - only show drafts if explicitly requested and for the current user
        if (showDrafts && User.Identity.IsAuthenticated)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            blogPostsQuery = blogPostsQuery.Where(b => b.AuthorId == currentUserId && b.Status == BlogPostStatus.Draft);
            ViewBag.ShowDrafts = true;
        }
        else
        {
            // By default, only show published posts
            blogPostsQuery = blogPostsQuery.Where(b => b.Status == BlogPostStatus.Published);
        }

        // Filter by search term
        if (!string.IsNullOrEmpty(searchTerm))
        {
            blogPostsQuery = blogPostsQuery.Where(b =>
                b.Title.Contains(searchTerm) ||
                (b.Film != null && b.Film.Title.Contains(searchTerm)));
        }

        // Filter by date
        var today = DateTime.Today;
        switch (dateFilter.ToLower())
        {
            case "today":
                blogPostsQuery = blogPostsQuery.Where(b => b.CreatedDate.Date == today);
                break;
            case "week":
                var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
                blogPostsQuery = blogPostsQuery.Where(b => b.CreatedDate >= startOfWeek);
                break;
            case "month":
                var startOfMonth = new DateTime(today.Year, today.Month, 1);
                blogPostsQuery = blogPostsQuery.Where(b => b.CreatedDate >= startOfMonth);
                break;
            case "year":
                var startOfYear = new DateTime(today.Year, 1, 1);
                blogPostsQuery = blogPostsQuery.Where(b => b.CreatedDate >= startOfYear);
                break;
        }

        // Sorting
        switch (sortBy)
        {
            case "date_asc":
                blogPostsQuery = blogPostsQuery.OrderBy(b => b.CreatedDate);
                break;
            case "title_asc":
                blogPostsQuery = blogPostsQuery.OrderBy(b => b.Title);
                break;
            case "title_desc":
                blogPostsQuery = blogPostsQuery.OrderByDescending(b => b.Title);
                break;
            default: // date_desc
                blogPostsQuery = blogPostsQuery.OrderByDescending(b => b.CreatedDate);
                break;
        }

        // Pagination
        var totalPosts = await blogPostsQuery.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalPosts / PageSize);
        page = Math.Min(page, Math.Max(1, totalPages));

        var blogPosts = await blogPostsQuery
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        // ViewBag assignments
        ViewBag.SearchTerm = searchTerm;
        ViewBag.DateFilter = dateFilter;
        ViewBag.SortBy = sortBy;
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalPosts = totalPosts;
        ViewBag.HasPreviousPage = page > 1;
        ViewBag.HasNextPage = page < totalPages;
        ViewBag.MyPosts = myPosts && User.Identity.IsAuthenticated;
        ViewBag.ShowDrafts = showDrafts && User.Identity.IsAuthenticated;

        return View(blogPosts);
    }

    // GET: Blog/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        await SetAvatarUrl();

        if (id == null) return NotFound();

        var blogPost = await _context.BlogPosts
            .Include(b => b.Film)
            .FirstOrDefaultAsync(m => m.Id == id);

        return blogPost == null ? NotFound() : View(blogPost);
    }

    // GET: Blog/Create
    [Authorize]
    public async Task<IActionResult> Create()
    {
        await SetAvatarUrl();
        ViewBag.Films = _context.Films.ToList();
        return View(new BlogPost());
    }

    // POST: Blog/Create
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BlogPost blogPost, string action)
    {
        if (ModelState.IsValid)
        {
            var user = await _userManager.GetUserAsync(User);
            blogPost.AuthorId = user?.Id;

            // Set status based on action
            blogPost.Status = action == "publish" ? BlogPostStatus.Published : BlogPostStatus.Draft;

            // Update created date only when publishing
            if (blogPost.Status == BlogPostStatus.Published)
            {
                blogPost.CreatedDate = DateTime.Now;
            }

            _context.Add(blogPost);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        ViewBag.Films = await _context.Films.ToListAsync();
        return View(blogPost);
    }

    // GET: Blog/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        if (!await CanEditBlogPost(id.Value))
        {
            return Forbid();
        }

        var blogPost = await _context.BlogPosts.FindAsync(id);
        if (blogPost == null) return NotFound();

        ViewBag.Films = await _context.Films.ToListAsync();
        ViewBag.Referer = Request.Headers["Referer"].ToString(); // Thêm dòng này
        return View(blogPost);
    }

    // POST: Blog/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BlogPost blogPost, string action)
    {
        if (id != blogPost.Id) return NotFound();

        if (!await CanEditBlogPost(id))
        {
            return Forbid();
        }

        if (ModelState.IsValid)
        {
            try
            {
                var existingBlogPost = await _context.BlogPosts.FindAsync(id);
                if (existingBlogPost == null)
                {
                    return NotFound();
                }

                existingBlogPost.Title = blogPost.Title;
                existingBlogPost.Content = blogPost.Content;
                existingBlogPost.FilmId = blogPost.FilmId;
                existingBlogPost.LastUpdated = DateTime.Now;

                // Update status based on action
                existingBlogPost.Status = action == "publish" ? BlogPostStatus.Published : BlogPostStatus.Draft;

                // Update created date if publishing for the first time
                if (existingBlogPost.Status == BlogPostStatus.Published && existingBlogPost.CreatedDate == default)
                {
                    existingBlogPost.CreatedDate = DateTime.Now;
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BlogPostExists(blogPost.Id))
                    return NotFound();
                throw;
            }
        }

        ViewBag.Films = await _context.Films.ToListAsync();
        return View(blogPost);
    }

    // GET: Blog/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        if (!await CanEditBlogPost(id.Value))
        {
            return Forbid();
        }

        var blogPost = await _context.BlogPosts
            .Include(b => b.Film)
            .Include(b => b.Author)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (blogPost == null) return NotFound();

        ViewBag.Referer = Request.Headers["Referer"].ToString(); // Thêm dòng này
        return View(blogPost);
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

    // Thêm method kiểm tra quyền
    private async Task<bool> CanEditBlogPost(int blogPostId)
    {
        if (User.IsInRole("Admin") || User.IsInRole("Manager"))
        {
            return true;
        }

        var blogPost = await _context.BlogPosts.FindAsync(blogPostId);
        if (blogPost == null) return false;

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return blogPost.AuthorId == currentUserId;
    }
    private async Task SetAvatarUrl()
    {
        var user = await _userManager.GetUserAsync(User);
        ViewBag.AvatarUrl = user?.AvatarUrl ?? "/image/logo/avatar_default.jpg";
    }

    private IQueryable<BlogPost> ApplySorting(IQueryable<BlogPost> query, string sortBy, string sortOrder)
    {
        return sortBy.ToLower() switch
        {
            "title" => sortOrder == "asc"
                ? query.OrderBy(b => b.Title)
                : query.OrderByDescending(b => b.Title),

            "date" => sortOrder == "asc"
                ? query.OrderBy(b => b.CreatedDate)
                : query.OrderByDescending(b => b.CreatedDate),

            _ => query.OrderByDescending(b => b.CreatedDate) // Default sort
        };
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadImage(IFormFile image)
    {
        if (image == null || image.Length == 0)
            return Json(new { success = 0, message = "No image uploaded" });

        // Validate file type
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
        var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
            return Json(new { success = 0, message = "Invalid file type. Only JPG, PNG, GIF are allowed." });

        // Validate file size (5MB max)
        if (image.Length > 5 * 1024 * 1024)
            return Json(new { success = 0, message = "File too large. Maximum size is 5MB." });

        try
        {
            // Create uploads folder if not exists
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            // Generate unique filename
            var uniqueFileName = Guid.NewGuid().ToString() + extension;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(fileStream);
            }

            // Return relative URL
            var imageUrl = Url.Content($"~/uploads/{uniqueFileName}");
            return Json(new
            {
                success = 1,
                file = new
                {
                    url = imageUrl
                }
            });
        }
        catch (Exception ex)
        {
            return Json(new
            {
                success = 0,
                message = "Error uploading image: " + ex.Message
            });
        }
    }
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(int id)
    {
        if (!await CanEditBlogPost(id))
        {
            return Forbid();
        }

        var blogPost = await _context.BlogPosts.FindAsync(id);
        if (blogPost == null)
        {
            return NotFound();
        }

        // Update status to Published and set created date if it's the first time
        blogPost.Status = BlogPostStatus.Published;
        if (blogPost.CreatedDate == default)
        {
            blogPost.CreatedDate = DateTime.Now;
        }
        blogPost.LastUpdated = DateTime.Now;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetAsDraft(int id)
    {
        if (!await CanEditBlogPost(id))
        {
            return Forbid();
        }

        var blogPost = await _context.BlogPosts.FindAsync(id);
        if (blogPost == null)
        {
            return NotFound();
        }

        // Chuyển bài viết đã publish về trạng thái draft
        blogPost.Status = BlogPostStatus.Draft;
        blogPost.LastUpdated = DateTime.Now;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Bài viết đã được chuyển về trạng thái nháp!";
        return RedirectToAction(nameof(Index));
    }
}