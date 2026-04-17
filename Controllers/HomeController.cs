using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebMkt.Data;
using WebMkt.Models;

namespace WebMkt.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _config;

    public HomeController(ApplicationDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    public async Task<IActionResult> Index()
    {
        var posts = await _context.Posts
            .Where(p => p.IsPublished)
            .OrderByDescending(p => p.CreatedAt)
            .Take(20)
            .ToListAsync();

        return View(posts);
    }

    [Route("post/{slug}")]
    public async Task<IActionResult> Post(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return NotFound();
        }

        var post = await _context.Posts
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Slug == slug && p.IsPublished);

        if (post == null)
        {
            return NotFound();
        }

        ViewBag.GoogleLeadMagnetHtml = _config.GetValue<string>("GoogleLeadMagnet:EmbedHtml") ?? string.Empty;
        return View(post);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
