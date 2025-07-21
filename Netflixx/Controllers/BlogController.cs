using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
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
            .Include(b => b.Likes) // Thêm include cho Likes
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
            case "like_desc":
                blogPostsQuery = blogPostsQuery
                    .Include(b => b.Likes)
                    .OrderByDescending(b => b.Likes.Count(l => l.IsLike));
                break;
            case "dislike_desc":
                blogPostsQuery = blogPostsQuery
                    .Include(b => b.Likes)
                    .OrderByDescending(b => b.Likes.Count(l => !l.IsLike));
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
            .Select(b => new {
                BlogPost = b,
                LikeCount = b.Likes.Count(l => l.IsLike),
                DislikeCount = b.Likes.Count(l => !l.IsLike)
            })
            //.OrderBy(x => x.LikeCount) // hoặc OrderByDescending tùy vào sortBy
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .Select(x => x.BlogPost)
            .ToListAsync();

        // Set CurrentUserVote for each post if user is authenticated
        if (User.Identity.IsAuthenticated)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userLikes = await _context.BlogLikes
                .Where(l => l.UserId == userId && blogPosts.Select(b => b.Id).Contains(l.BlogPostId))
                .ToListAsync();

            foreach (var post in blogPosts)
            {
                var userLike = userLikes.FirstOrDefault(l => l.BlogPostId == post.Id);
                post.CurrentUserVote = userLike?.IsLike;
            }
        }

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
        if (id == null) return NotFound();

        var blogPost = await _context.BlogPosts
            .Include(b => b.Film)
            .Include(b => b.Comments)
                .ThenInclude(c => c.Author)
            .Include(b => b.Comments)
                .ThenInclude(c => c.Replies)
                    .ThenInclude(r => r.Author)
            .Include(b => b.Likes)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (blogPost == null) return NotFound();

        if (User.Identity.IsAuthenticated)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userLike = blogPost.Likes.FirstOrDefault(l => l.UserId == userId);
            blogPost.CurrentUserVote = userLike?.IsLike;
        }

        return View(blogPost);
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
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(int blogPostId, string content, int? parentCommentId = null)
    {
        if (!User.Identity.IsAuthenticated)
        {
            return Json(new { success = false, message = "You must be logged in to comment." });
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            return Json(new { success = false, message = "Comment content cannot be empty." });
        }

        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var blogPost = await _context.BlogPosts.FindAsync(blogPostId);

            if (blogPost == null)
            {
                return Json(new { success = false, message = "Blog post not found." });
            }

            var comment = new BlogComment
            {
                Content = content,
                AuthorId = userId,
                BlogPostId = blogPostId,
                ParentCommentId = parentCommentId,
                CreatedDate = DateTime.Now
            };

            _context.BlogComments.Add(comment);
            await _context.SaveChangesAsync();

            // Nếu là AJAX request, trả về JSON
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = "Comment added successfully." });
            }

            // Nếu không phải AJAX, redirect như cũ
            return RedirectToAction("Details", new { id = blogPostId });
        }
        catch (Exception ex)
        {
            // Log error
            return Json(new { success = false, message = "An error occurred while adding the comment." });
        }
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> ToggleLike(int blogPostId, bool isLike)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var existingLike = await _context.BlogLikes
            .FirstOrDefaultAsync(l => l.BlogPostId == blogPostId && l.UserId == userId);

        if (existingLike != null)
        {
            if (existingLike.IsLike == isLike)
            {
                _context.BlogLikes.Remove(existingLike);
            }
            else
            {
                existingLike.IsLike = isLike;
            }
        }
        else
        {
            _context.BlogLikes.Add(new BlogLike
            {
                BlogPostId = blogPostId,
                UserId = userId,
                IsLike = isLike
            });
        }

        await _context.SaveChangesAsync();

        var likeCount = await _context.BlogLikes.CountAsync(l => l.BlogPostId == blogPostId && l.IsLike);
        var dislikeCount = await _context.BlogLikes.CountAsync(l => l.BlogPostId == blogPostId && !l.IsLike);

        return Json(new { success = true, likeCount, dislikeCount });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnableEditComment(int commentId)
    {
        try
        {
            // Lấy userId trước khi query
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(currentUserId))
            {
                return Json(new { success = false, message = "User not authenticated." });
            }

            var comment = await _context.BlogComments
                .Include(c => c.Author)
                .FirstOrDefaultAsync(c => c.Id == commentId);

            if (comment == null)
            {
                return Json(new { success = false, message = "Comment not found." });
            }

            // Check permission
            if (!User.IsInRole("Admin") && !User.IsInRole("Manager") &&
                comment.AuthorId != currentUserId)
            {
                return Json(new { success = false, message = "You don't have permission to edit this comment." });
            }

            // Reset all comments' IsEditing status for this user - SỬA LỖI TẠI ĐÂY
            var userComments = await _context.BlogComments
                .Where(c => c.AuthorId == currentUserId) // Sử dụng biến đã kiểm tra null
                .ToListAsync();

            foreach (var c in userComments)
            {
                c.IsEditing = false;
            }

            // Set the current comment to editing mode
            comment.IsEditing = true;
            await _context.SaveChangesAsync();

            // Return updated comments HTML
            var blogPost = await _context.BlogPosts
                .Include(bp => bp.Comments)
                    .ThenInclude(c => c.Author)
                .Include(bp => bp.Comments)
                    .ThenInclude(c => c.Replies)
                        .ThenInclude(r => r.Author)
                .FirstOrDefaultAsync(bp => bp.Id == comment.BlogPostId);

            var topLevelComments = blogPost.Comments
                .Where(c => c.ParentCommentId == null)
                .OrderByDescending(c => c.CreatedDate)
                .ToList();

            return PartialView("_CommentsPartial", topLevelComments);
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "An error occurred while enabling edit mode." });
        }
    }

    // Helper method để đánh dấu comment cần edit
    private void MarkCommentForEdit(BlogComment comment, int editCommentId)
    {
        if (comment.Id == editCommentId)
        {
            comment.IsEditing = true;
        }

        foreach (var reply in comment.Replies)
        {
            MarkCommentForEdit(reply, editCommentId);
        }
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateComment(int commentId, string content)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return Json(new { success = false, message = "Comment content cannot be empty." });
            }

            var comment = await _context.BlogComments
                .Include(c => c.Author)
                .FirstOrDefaultAsync(c => c.Id == commentId);

            if (comment == null)
            {
                return Json(new { success = false, message = "Comment not found." });
            }

            // Check permission
            if (!User.IsInRole("Admin") && !User.IsInRole("Manager") &&
                comment.AuthorId != User.FindFirst(ClaimTypes.NameIdentifier)?.Value)
            {
                return Json(new { success = false, message = "You don't have permission to edit this comment." });
            }

            comment.Content = content;
            comment.IsEditing = false;
            comment.LastUpdated = DateTime.Now;

            await _context.SaveChangesAsync();

            // Return updated comments HTML
            var blogPost = await _context.BlogPosts
                .Include(bp => bp.Comments)
                    .ThenInclude(c => c.Author)
                .Include(bp => bp.Comments)
                    .ThenInclude(c => c.Replies)
                        .ThenInclude(r => r.Author)
                .FirstOrDefaultAsync(bp => bp.Id == comment.BlogPostId);

            var topLevelComments = blogPost.Comments
                .Where(c => c.ParentCommentId == null)
                .OrderByDescending(c => c.CreatedDate)
                .ToList();

            return PartialView("_CommentsPartial", topLevelComments);
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "An error occurred while updating the comment." });
        }
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteComment(int commentId)
    {
        try
        {
            var comment = await _context.BlogComments
                .Include(c => c.Replies)
                .FirstOrDefaultAsync(c => c.Id == commentId);

            if (comment == null)
            {
                return Json(new { success = false, message = "Comment not found." });
            }

            // Check permission
            if (!User.IsInRole("Admin") && !User.IsInRole("Manager") &&
                comment.AuthorId != User.FindFirst(ClaimTypes.NameIdentifier)?.Value)
            {
                return Json(new { success = false, message = "You don't have permission to delete this comment." });
            }

            // Hard delete: xóa comment và toàn bộ nhánh con
            await DeleteCommentAndChildrenAsync(comment);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Comment and all its replies deleted successfully." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "An error occurred while deleting the comment." });
        }
    }


    // Helper method để lấy comments của blog post
    private async Task<List<BlogComment>> GetCommentsForBlogPost(int blogPostId)
    {
        return await _context.BlogComments
            .Include(c => c.Author)
            .Include(c => c.Replies)
                .ThenInclude(r => r.Author)
            .Where(c => c.BlogPostId == blogPostId && c.ParentCommentId == null)
            .OrderByDescending(c => c.CreatedDate)
            .ToListAsync();
    }

    // Helper method để render partial view thành string
    private async Task<string> RenderPartialViewToString(string viewName, object model)
    {
        ViewData.Model = model;
        using (var writer = new StringWriter())
        {
            var viewEngine = HttpContext.RequestServices.GetService<ICompositeViewEngine>();
            var viewContext = new ViewContext(ControllerContext, viewEngine.FindView(ControllerContext, viewName, false).View, ViewData, TempData, writer, new HtmlHelperOptions());

            await viewContext.View.RenderAsync(viewContext);
            return writer.GetStringBuilder().ToString();
        }
    }

    // Thêm method để xóa comment với soft delete (tùy chọn)
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SoftDeleteComment(int commentId)
    {
        try
        {
            var comment = await _context.BlogComments.FindAsync(commentId);

            if (comment == null)
            {
                return Json(new { success = false, message = "Comment not found" });
            }

            // Kiểm tra quyền
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            bool canDelete = User.IsInRole("Admin") ||
                            User.IsInRole("Manager") ||
                            comment.AuthorId == currentUserId;

            if (!canDelete)
            {
                return Json(new { success = false, message = "You don't have permission to delete this comment" });
            }

            // Soft delete - chỉ thay đổi nội dung thay vì xóa hẳn
            comment.Content = "[This comment has been deleted]";
            comment.AuthorId = null; // Hoặc giữ nguyên tùy business logic

            await _context.SaveChangesAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var updatedComments = await GetCommentsForBlogPost(comment.BlogPostId);
                var commentsHtml = await RenderPartialViewToString("_CommentsPartial", updatedComments);

                return Json(new
                {
                    success = true,
                    message = "Comment deleted successfully",
                    commentsHtml = commentsHtml
                });
            }

            TempData["SuccessMessage"] = "Comment deleted successfully";
            return RedirectToAction("Details", new { id = comment.BlogPostId });
        }
        catch (Exception ex)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = false, message = "An error occurred while deleting the comment" });
            }

            TempData["ErrorMessage"] = "An error occurred while deleting the comment";
            return RedirectToAction("Index");
        }
    }

    // Thêm method này vào BlogController để hỗ trợ xóa nhiều comment cùng lúc

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkDeleteComments(int[] commentIds)
    {
        try
        {
            if (commentIds == null || commentIds.Length == 0)
            {
                return Json(new { success = false, message = "No comments selected." });
            }

            var comments = await _context.BlogComments
                .Include(c => c.Replies)
                .Where(c => commentIds.Contains(c.Id))
                .ToListAsync();

            foreach (var comment in comments)
            {
                if (!User.IsInRole("Admin") && !User.IsInRole("Manager") &&
                    comment.AuthorId != User.FindFirst(ClaimTypes.NameIdentifier)?.Value)
                {
                    continue;
                }

                await DeleteCommentAndChildrenAsync(comment);
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Comments deleted successfully." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "An error occurred while deleting comments." });
        }
    }

    // Helper method để thu thập tất cả replies recursively
    private void CollectRepliesRecursively(BlogComment comment, List<BlogComment> allComments)
    {
        if (comment.Replies?.Any() == true)
        {
            foreach (var reply in comment.Replies)
            {
                allComments.Add(reply);
                CollectRepliesRecursively(reply, allComments);
            }
        }
    }

    // Method để lấy thống kê comments (hữu ích cho admin dashboard)
    [Authorize(Roles = "Admin,Manager")]
    [HttpGet]
    public async Task<IActionResult> GetCommentStats()
    {
        try
        {
            var stats = new
            {
                TotalComments = await _context.BlogComments.CountAsync(),
                TotalReplies = await _context.BlogComments.CountAsync(c => c.ParentCommentId != null),
                CommentsToday = await _context.BlogComments.CountAsync(c => c.CreatedDate.Date == DateTime.Today),
                TopCommenters = await _context.BlogComments
                    .Include(c => c.Author)
                    .GroupBy(c => c.AuthorId)
                    .Select(g => new
                    {
                        AuthorName = g.First().Author.UserName,
                        CommentCount = g.Count()
                    })
                    .OrderByDescending(x => x.CommentCount)
                    .Take(5)
                    .ToListAsync(),
                CommentsByMonth = await _context.BlogComments
                    .Where(c => c.CreatedDate >= DateTime.Now.AddMonths(-6))
                    .GroupBy(c => new { c.CreatedDate.Year, c.CreatedDate.Month })
                    .Select(g => new
                    {
                        Month = $"{g.Key.Year}-{g.Key.Month:00}",
                        Count = g.Count()
                    })
                    .OrderBy(x => x.Month)
                    .ToListAsync()
            };

            return Json(stats);
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error retrieving comment statistics" });
        }
    }

    // Method để restore soft-deleted comments (nếu sử dụng soft delete)
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RestoreComment(int commentId)
    {
        try
        {
            var comment = await _context.BlogComments.FindAsync(commentId);

            if (comment == null)
            {
                return Json(new { success = false, message = "Comment not found" });
            }

            // Restore logic (tùy thuộc vào cách implement soft delete)
            if (comment.Content == "[This comment has been deleted]")
            {
                return Json(new { success = false, message = "Cannot restore this comment - original content not available" });
            }

            // Nếu có backup content field
            // comment.Content = comment.BackupContent;
            // comment.IsDeleted = false;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Comment restored successfully" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Error restoring comment" });
        }
    }
    private async Task DeleteCommentAndChildrenAsync(BlogComment comment)
    {
        // Nạp replies nếu chưa có (phòng trường hợp chưa Include)
        if (comment.Replies == null || !comment.Replies.Any())
        {
            comment.Replies = await _context.BlogComments
                .Where(c => c.ParentCommentId == comment.Id)
                .ToListAsync();
        }

        foreach (var reply in comment.Replies.ToList())
        {
            await DeleteCommentAndChildrenAsync(reply);
        }

        _context.BlogComments.Remove(comment);
    }
}