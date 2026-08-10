using System.Diagnostics;
using System.Text.RegularExpressions;
using AgroForum.Data;
using AgroForum.Helpers;
using AgroForum.Models;
using AgroForum.ViewModels.Home;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgroForum.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var recentPosts = await _context.ForumPosts
                .AsNoTracking()
                .AsSplitQuery()
                .Include(post => post.Author)
                .Include(post => post.Comments)
                .Include(post => post.PostTags)
                    .ThenInclude(postTag => postTag.ForumTag)
                .Where(post => !post.IsDeleted)
                .OrderByDescending(post => post.IsPinned)
                .ThenByDescending(post => post.CreatedAt)
                .Take(3)
                .ToListAsync();

            var popularTags = await _context.ForumTags
                .AsNoTracking()
                .Select(tag => new HomeCategoryViewModel
                {
                    Name = tag.Name,
                    Slug = tag.Slug,
                    DiscussionCount = tag.PostTags.Count(postTag => !postTag.ForumPost.IsDeleted)
                })
                .OrderByDescending(tag => tag.DiscussionCount)
                .ThenBy(tag => tag.Name)
                .Take(4)
                .ToListAsync();

            var model = new HomeIndexViewModel
            {
                MemberCount = await _context.Users.AsNoTracking().CountAsync(),
                DiscussionCount = await _context.ForumPosts.AsNoTracking().CountAsync(post => !post.IsDeleted),
                ReplyCount = await _context.ForumComments.AsNoTracking().CountAsync(comment => !comment.IsDeleted),
                Categories = popularTags,
                RecentDiscussions = recentPosts.Select(post => new HomeDiscussionViewModel
                {
                    Id = post.Id,
                    Title = post.Title,
                    Preview = BuildPreview(post.Content),
                    AuthorName = GetDisplayName(post.Author, post.IsAnonymous),
                    AuthorId = post.IsAnonymous ? null : post.AuthorId,
                    CreatedAt = post.CreatedAt,
                    ReplyCount = post.Comments.Count(comment => !comment.IsDeleted),
                    TagName = post.PostTags.Select(postTag => postTag.ForumTag.Name).FirstOrDefault(),
                    TagSlug = post.PostTags.Select(postTag => postTag.ForumTag.Slug).FirstOrDefault()
                }).ToList()
            };

            return View(model);
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

        private static string GetDisplayName(ApplicationUser? user, bool isAnonymous)
        {
            if (isAnonymous)
            {
                return "Anonymous farmer";
            }

            if (user == null)
            {
                return "Community member";
            }

            return CommunityDisplayName.For(user);
        }

        private static string BuildPreview(string content)
        {
            var preview = Regex.Replace(content, "\\s+", " ").Trim();
            return preview.Length <= 105 ? preview : $"{preview[..105]}...";
        }
    }
}
