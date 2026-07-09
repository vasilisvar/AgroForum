using System.Text.RegularExpressions;
using AgroForum.Data;
using AgroForum.Helpers;
using AgroForum.Models;
using AgroForum.Models.Forum;
using AgroForum.ViewModels.Forum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgroForum.Controllers
{
    public class ForumController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ForumController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(string? search, string? tag)
        {
            var postsQuery = _context.ForumPosts
                .AsNoTracking()
                .Include(post => post.Author)
                .Include(post => post.PostTags)
                    .ThenInclude(postTag => postTag.ForumTag)
                .Include(post => post.Comments)
                .Include(post => post.Favorites)
                .Where(post => !post.IsDeleted);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                postsQuery = postsQuery.Where(post =>
                    post.Title.Contains(term) ||
                    post.Content.Contains(term) ||
                    post.PostTags.Any(postTag => postTag.ForumTag.Name.Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(tag))
            {
                var selectedTag = tag.Trim();
                postsQuery = postsQuery.Where(post =>
                    post.PostTags.Any(postTag => postTag.ForumTag.Slug == selectedTag || postTag.ForumTag.Name == selectedTag));
            }

            var posts = await postsQuery
                .OrderByDescending(post => post.CreatedAt)
                .ToListAsync();

            var availableTags = await _context.ForumTags
                .AsNoTracking()
                .OrderBy(tagItem => tagItem.Name)
                .Select(tagItem => tagItem.Name)
                .ToListAsync();

            var model = new ForumIndexViewModel
            {
                Search = search,
                Tag = tag,
                AvailableTags = availableTags,
                Posts = posts.Select(post => new ForumPostSummaryViewModel
                {
                    Id = post.Id,
                    Title = post.Title,
                    Preview = BuildPreview(post.Content),
                    AuthorName = GetDisplayName(post.Author, post.IsAnonymous),
                    IsAnonymous = post.IsAnonymous,
                    IsLocked = post.IsLocked,
                    CreatedAt = post.CreatedAt,
                    CommentCount = post.Comments.Count(comment => !comment.IsDeleted),
                    FavoriteCount = post.Favorites.Count,
                    Tags = post.PostTags
                        .Select(postTag => postTag.ForumTag.Name)
                        .OrderBy(name => name)
                        .ToList()
                }).ToList()
            };

            return View(model);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var post = await _context.ForumPosts
                .AsNoTracking()
                .Include(item => item.Author)
                .Include(item => item.PostTags)
                    .ThenInclude(postTag => postTag.ForumTag)
                .Include(item => item.Comments)
                    .ThenInclude(comment => comment.Author)
                .FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted);

            if (post == null)
            {
                return NotFound();
            }

            var model = new ForumPostDetailsViewModel
            {
                Id = post.Id,
                Title = post.Title,
                Content = post.Content,
                AuthorName = GetDisplayName(post.Author, post.IsAnonymous),
                IsAnonymous = post.IsAnonymous,
                IsLocked = post.IsLocked,
                CreatedAt = post.CreatedAt,
                UpdatedAt = post.UpdatedAt,
                Tags = post.PostTags
                    .Select(postTag => new ForumTagViewModel
                    {
                        Id = postTag.ForumTag.Id,
                        Name = postTag.ForumTag.Name,
                        Slug = postTag.ForumTag.Slug
                    })
                    .OrderBy(tag => tag.Name)
                    .ToList(),
                Comments = post.Comments
                    .Where(comment => !comment.IsDeleted)
                    .OrderBy(comment => comment.CreatedAt)
                    .Select(comment => new ForumCommentViewModel
                    {
                        Id = comment.Id,
                        Content = comment.Content,
                        AuthorName = GetDisplayName(comment.Author, isAnonymous: false),
                        CreatedAt = comment.CreatedAt,
                        UpdatedAt = comment.UpdatedAt,
                        IsDeleted = comment.IsDeleted,
                        DeletionReason = comment.DeletionReason
                    })
                    .ToList(),
                NewComment = new CreateForumCommentViewModel { ForumPostId = post.Id }
            };

            return View(model);
        }

        [Authorize]
        public IActionResult Create()
        {
            return View(new CreateForumPostViewModel());
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateForumPostViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Challenge();
            }

            var post = new ForumPost
            {
                Title = model.Title.Trim(),
                Content = model.Content.Trim(),
                IsAnonymous = model.IsAnonymous,
                AuthorId = userId,
                CreatedAt = DateTime.UtcNow
            };

            await AddTagsToPostAsync(post, model.Tags);

            _context.ForumPosts.Add(post);
            await _context.SaveChangesAsync();

            TempData["ForumMessage"] = "Your post has been published.";
            return RedirectToAction(nameof(Details), new { id = post.Id });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(CreateForumCommentViewModel model)
        {
            var post = await _context.ForumPosts
                .FirstOrDefaultAsync(item => item.Id == model.ForumPostId && !item.IsDeleted);

            if (post == null)
            {
                return NotFound();
            }

            if (post.IsLocked)
            {
                TempData["ForumError"] = "This post is locked, so new comments are not allowed.";
                return RedirectToAction(nameof(Details), new { id = post.Id });
            }

            if (!ModelState.IsValid)
            {
                TempData["ForumError"] = "Please write a valid comment before submitting.";
                return RedirectToAction(nameof(Details), new { id = post.Id });
            }

            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Challenge();
            }

            var comment = new ForumComment
            {
                ForumPostId = post.Id,
                Content = model.Content.Trim(),
                AuthorId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.ForumComments.Add(comment);
            await _context.SaveChangesAsync();

            TempData["ForumMessage"] = "Your comment has been added.";
            return RedirectToAction(nameof(Details), new { id = post.Id });
        }

        [Authorize]
        public async Task<IActionResult> Report(int? postId, int? commentId)
        {
            var model = await BuildReportContentModelAsync(postId, commentId);
            if (model == null)
            {
                return NotFound();
            }

            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Report(ReportContentViewModel model)
        {
            if (model.ForumPostId == null && model.ForumCommentId == null)
            {
                ModelState.AddModelError(string.Empty, "Choose a post or comment to report.");
            }

            var hydratedModel = await BuildReportContentModelAsync(model.ForumPostId, model.ForumCommentId);
            if (hydratedModel == null)
            {
                return NotFound();
            }

            hydratedModel.Reason = model.Reason;
            hydratedModel.Details = model.Details;

            if (!ModelState.IsValid)
            {
                return View(hydratedModel);
            }

            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Challenge();
            }

            var existingReport = await _context.ForumReports
                .AnyAsync(report =>
                    report.ReporterId == userId &&
                    report.Status == ForumReportStatuses.Pending &&
                    report.ForumPostId == model.ForumPostId &&
                    report.ForumCommentId == model.ForumCommentId);

            if (existingReport)
            {
                TempData["ForumMessage"] = "This content is already in the moderation queue from your report.";
                return RedirectToAction(nameof(Details), new { id = hydratedModel.ReturnForumPostId });
            }

            _context.ForumReports.Add(new ForumReport
            {
                ForumPostId = model.ForumPostId,
                ForumCommentId = model.ForumCommentId,
                ReporterId = userId,
                Reason = model.Reason.Trim(),
                Details = model.Details?.Trim(),
                Status = ForumReportStatuses.Pending,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            TempData["ForumMessage"] = "Thanks. The content has been sent to the moderation queue.";
            return RedirectToAction(nameof(Details), new { id = hydratedModel.ReturnForumPostId });
        }

        private async Task AddTagsToPostAsync(ForumPost post, string? tagList)
        {
            foreach (var tagName in ForumTagUtilities.ParseTagNames(tagList))
            {
                var slug = ForumTagUtilities.CreateSlug(tagName);
                var tag = await _context.ForumTags.FirstOrDefaultAsync(item => item.Slug == slug);

                if (tag == null)
                {
                    tag = new ForumTag
                    {
                        Name = tagName,
                        Slug = slug
                    };

                    _context.ForumTags.Add(tag);
                }

                post.PostTags.Add(new ForumPostTag
                {
                    ForumPost = post,
                    ForumTag = tag
                });
            }
        }

        private async Task<ReportContentViewModel?> BuildReportContentModelAsync(int? postId, int? commentId)
        {
            if (commentId.HasValue)
            {
                var comment = await _context.ForumComments
                    .AsNoTracking()
                    .Include(item => item.ForumPost)
                    .FirstOrDefaultAsync(item => item.Id == commentId.Value && !item.IsDeleted && !item.ForumPost.IsDeleted);

                if (comment == null)
                {
                    return null;
                }

                return new ReportContentViewModel
                {
                    ForumCommentId = comment.Id,
                    ReturnForumPostId = comment.ForumPostId,
                    TargetType = "Comment",
                    TargetTitle = comment.ForumPost.Title,
                    TargetPreview = BuildPreview(comment.Content)
                };
            }

            if (postId.HasValue)
            {
                var post = await _context.ForumPosts
                    .AsNoTracking()
                    .FirstOrDefaultAsync(item => item.Id == postId.Value && !item.IsDeleted);

                if (post == null)
                {
                    return null;
                }

                return new ReportContentViewModel
                {
                    ForumPostId = post.Id,
                    ReturnForumPostId = post.Id,
                    TargetType = "Post",
                    TargetTitle = post.Title,
                    TargetPreview = BuildPreview(post.Content)
                };
            }

            return null;
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

            var fullName = $"{user.FirstName} {user.LastName}".Trim();
            return string.IsNullOrWhiteSpace(fullName) ? user.UserName ?? "Community member" : fullName;
        }

        private static string BuildPreview(string content)
        {
            var preview = Regex.Replace(content, "\\s+", " ").Trim();
            return preview.Length <= 180 ? preview : $"{preview[..180]}...";
        }
    }
}
