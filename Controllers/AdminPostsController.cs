using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebMkt.Data;
using WebMkt.Models;
using WebMkt.Services;

namespace WebMkt.Controllers
{
    // [Authorize] - Bỏ comment để yêu cầu đăng nhập Admin
    public class AdminPostsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAiService _aiService;

        public AdminPostsController(ApplicationDbContext context, IAiService aiService)
        {
            _context = context;
            _aiService = aiService;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Posts.Include(p => p.Category).ToListAsync());
        }

        public IActionResult CreateWithAi()
        {
            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GenerateWithAi(string keyword, string tone)
        {
            try
            {
                var response = await _aiService.GenerateArticleAsync(keyword, tone);
                try {
                    response.ThumbnailUrl = await _aiService.GenerateImageAsync(keyword, response.Slug);
                } catch { 
                    // Ignore image failure
                }
                return Json(new { success = true, data = response });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GenerateOutlineWithAi(string keyword, string tone)
        {
            try
            {
                var response = await _aiService.GenerateOutlineAsync(keyword, tone);
                return Json(new { success = true, data = response });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GenerateArticleFromOutlineWithAi(string keyword, string tone, string outline)
        {
            try
            {
                var response = await _aiService.GenerateArticleFromOutlineAsync(keyword, tone, outline);
                try {
                    response.ThumbnailUrl = await _aiService.GenerateImageAsync(keyword, response.Slug);
                } catch { 
                    // Ignore image failure
                }
                return Json(new { success = true, data = response });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GenerateArticleFromUrlWithAi(string url, string tone, string instruction)
        {
            try
            {
                var response = await _aiService.GenerateArticleFromUrlAsync(url, tone, instruction);
                try {
                    response.ThumbnailUrl = await _aiService.GenerateImageAsync(response.Title ?? "competitor-article", response.Slug);
                } catch {
                    // Ignore image failure
                }
                return Json(new { success = true, data = response });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AjaxGenerateImageForPost(string keyword, string slug)
        {
            try
            {
                var resultUrl = await _aiService.GenerateImageAsync(keyword, slug);
                return Json(new { success = true, url = resultUrl });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AiRewriteContent(string content, string instruction)
        {
            try
            {
                var resultHtml = await _aiService.RewriteContentAsync(content, instruction);
                return Json(new { success = true, data = resultHtml });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public IActionResult BulkCreateWithAi()
        {
            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AutoSaveAiPost([FromForm] Post post)
        {
            if (!string.IsNullOrEmpty(post.SchemaMarkup) && post.SchemaMarkup.Length > 1000) post.SchemaMarkup = post.SchemaMarkup.Substring(0, 999);
            if (!string.IsNullOrEmpty(post.MetaDescription) && post.MetaDescription.Length > 500) post.MetaDescription = post.MetaDescription.Substring(0, 499);
            if (!string.IsNullOrEmpty(post.MetaTitle) && post.MetaTitle.Length > 255) post.MetaTitle = post.MetaTitle.Substring(0, 254);
            if (!string.IsNullOrEmpty(post.Title) && post.Title.Length > 255) post.Title = post.Title.Substring(0, 254);
            if (!string.IsNullOrEmpty(post.Slug) && post.Slug.Length > 255) post.Slug = post.Slug.Substring(0, 254);

            ModelState.Clear();
            TryValidateModel(post);
            ModelState.Remove("Category");
            ModelState.Remove("PostTags");

            if (ModelState.IsValid)
            {
                var categoryExists = await _context.Categories.AnyAsync(c => c.Id == post.CategoryId);
                if (!categoryExists)
                {
                    var defaultCategory = await _context.Categories.FirstOrDefaultAsync();
                    if (defaultCategory == null)
                    {
                        defaultCategory = new Category { Name = "Chung", Slug = "chung", Description = "" };
                        _context.Categories.Add(defaultCategory);
                        await _context.SaveChangesAsync();
                    }
                    post.CategoryId = defaultCategory.Id;
                }

                post.Description ??= "";
                post.MetaTitle ??= "";
                post.MetaDescription ??= "";
                post.MetaKeywords ??= "";
                post.SchemaMarkup ??= "";
                post.ThumbnailUrl ??= "";

                _context.Add(post);
                await _context.SaveChangesAsync();
                return Json(new { success = true, postId = post.Id, title = post.Title });
            }

            var errors = string.Join("; ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
            return Json(new { success = false, message = "Dữ liệu không hợp lệ. " + errors });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Post post)
        {
            // Truncate long AI fields to match DB constraints
            if (!string.IsNullOrEmpty(post.SchemaMarkup) && post.SchemaMarkup.Length > 1000) post.SchemaMarkup = post.SchemaMarkup.Substring(0, 999);
            if (!string.IsNullOrEmpty(post.MetaDescription) && post.MetaDescription.Length > 500) post.MetaDescription = post.MetaDescription.Substring(0, 499);
            if (!string.IsNullOrEmpty(post.MetaTitle) && post.MetaTitle.Length > 255) post.MetaTitle = post.MetaTitle.Substring(0, 254);
            if (!string.IsNullOrEmpty(post.Title) && post.Title.Length > 255) post.Title = post.Title.Substring(0, 254);
            if (!string.IsNullOrEmpty(post.Slug) && post.Slug.Length > 255) post.Slug = post.Slug.Substring(0, 254);

            ModelState.Clear();
            TryValidateModel(post);

            ModelState.Remove("Category");
            ModelState.Remove("PostTags");
            
            if (ModelState.IsValid)
            {
                // Ensure a valid Category exists to prevent Foreign Key constraint errors
                var categoryExists = await _context.Categories.AnyAsync(c => c.Id == post.CategoryId);
                if (!categoryExists)
                {
                    var defaultCategory = await _context.Categories.FirstOrDefaultAsync();
                    if (defaultCategory == null)
                    {
                        defaultCategory = new Category { Name = "Chung", Slug = "chung", Description = "" };
                        _context.Categories.Add(defaultCategory);
                        await _context.SaveChangesAsync();
                    }
                    post.CategoryId = defaultCategory.Id;
                }

                // Prevent SQL insertion errors for NOT NULL constraints
                post.Description ??= "";
                post.MetaTitle ??= "";
                post.MetaDescription ??= "";
                post.MetaKeywords ??= "";
                post.SchemaMarkup ??= "";

                _context.Add(post);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Details), new { id = post.Id });
            }
            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", post.CategoryId);
            return View("CreateWithAi", post);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var post = await _context.Posts
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.Id == id);
            
            if (post == null)
            {
                return NotFound();
            }

            return View(post);
        }

        // GET: AdminPosts/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var post = await _context.Posts.FindAsync(id);
            if (post == null)
            {
                return NotFound();
            }
            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", post.CategoryId);
            return View(post);
        }

        // POST: AdminPosts/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Post post)
        {
            if (id != post.Id)
            {
                return NotFound();
            }

            ModelState.Remove("Category");
            ModelState.Remove("PostTags");

            if (ModelState.IsValid)
            {
                try
                {
                    post.Description ??= "";
                    post.MetaTitle ??= "";
                    post.MetaDescription ??= "";
                    post.MetaKeywords ??= "";
                    post.SchemaMarkup ??= "";

                    var existingPost = await _context.Posts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
                    if (existingPost != null)
                    {
                        post.CreatedAt = existingPost.CreatedAt;
                    }
                    post.UpdatedAt = DateTime.Now;

                    _context.Update(post);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PostExists(post.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", post.CategoryId);
            return View(post);
        }

        // GET: AdminPosts/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var post = await _context.Posts
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (post == null)
            {
                return NotFound();
            }

            return View(post);
        }

        // POST: AdminPosts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post != null)
            {
                _context.Posts.Remove(post);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool PostExists(int id)
        {
            return _context.Posts.Any(e => e.Id == id);
        }
    }
}
