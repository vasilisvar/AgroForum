using System.Text.RegularExpressions;
using AgroForum.Constants;
using AgroForum.Data;
using AgroForum.Helpers;
using AgroForum.Models;
using AgroForum.Models.Forum;
using AgroForum.Services.PostImages;
using AgroForum.ViewModels.Forum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgroForum.Controllers
{
    public class ForumController : Controller
    {
        private const int ForumPageSize = 6;

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly PostImageStorage _postImageStorage;

        public ForumController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            PostImageStorage postImageStorage)
        {
            _context = context;
            _userManager = userManager;
            _postImageStorage = postImageStorage;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(string? search, string? tag, string? sort, int page = 1)
        {
            var currentUserId = _userManager.GetUserId(User);
            var normalizedSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
            var normalizedTag = string.IsNullOrWhiteSpace(tag) ? null : tag.Trim();
            var selectedSort = sort?.Trim().ToLowerInvariant() switch
            {
                "likes" => "likes",
                "comments" => "comments",
                _ => "newest"
            };

            var postsQuery = _context.ForumPosts
                .AsNoTracking()
                .AsSplitQuery()
                .Include(post => post.Author)
                .Include(post => post.PostTags)
                    .ThenInclude(postTag => postTag.ForumTag)
                .Include(post => post.Comments)
                .Include(post => post.Likes)
                .Include(post => post.Favorites)
                .Where(post => !post.IsDeleted);

            if (normalizedSearch != null)
            {
                postsQuery = postsQuery.Where(post =>
                    post.Title.Contains(normalizedSearch) ||
                    post.Content.Contains(normalizedSearch) ||
                    post.PostTags.Any(postTag => postTag.ForumTag.Name.Contains(normalizedSearch)));
            }

            if (normalizedTag != null)
            {
                postsQuery = postsQuery.Where(post =>
                    post.PostTags.Any(postTag =>
                        postTag.ForumTag.Slug == normalizedTag ||
                        postTag.ForumTag.Name == normalizedTag));
            }

            var totalResults = await postsQuery.CountAsync();
            var totalPages = (int)Math.Ceiling(totalResults / (double)ForumPageSize);
            var currentPage = totalPages == 0
                ? 1
                : Math.Min(Math.Max(page, 1), totalPages);

            var orderedPosts = selectedSort switch
            {
                "likes" => postsQuery
                    .OrderByDescending(post => post.IsPinned)
                    .ThenByDescending(post => post.Likes.Count)
                    .ThenByDescending(post => post.CreatedAt)
                    .ThenByDescending(post => post.Id),
                "comments" => postsQuery
                    .OrderByDescending(post => post.IsPinned)
                    .ThenByDescending(post => post.Comments.Count(comment => !comment.IsDeleted))
                    .ThenByDescending(post => post.CreatedAt)
                    .ThenByDescending(post => post.Id),
                _ => postsQuery
                    .OrderByDescending(post => post.IsPinned)
                    .ThenByDescending(post => post.CreatedAt)
                    .ThenByDescending(post => post.Id)
            };

            var posts = await orderedPosts
                .Skip((currentPage - 1) * ForumPageSize)
                .Take(ForumPageSize)
                .ToListAsync();

            var moderatorIds = await GetModeratorIdsAsync(posts.Select(post => post.AuthorId));

            var availableTags = await _context.ForumTags
                .AsNoTracking()
                .OrderBy(tagItem => tagItem.Name)
                .Select(tagItem => new ForumTagViewModel
                {
                    Id = tagItem.Id,
                    Name = tagItem.Name,
                    Slug = tagItem.Slug
                })
                .ToListAsync();

            var model = new ForumIndexViewModel
            {
                Search = normalizedSearch,
                Tag = normalizedTag,
                Sort = selectedSort,
                CurrentPage = currentPage,
                TotalPages = totalPages,
                TotalResults = totalResults,
                AvailableTags = availableTags,
                Posts = posts.Select(post => new ForumPostSummaryViewModel
                {
                    Id = post.Id,
                    Title = post.Title,
                    Preview = BuildPreview(post.Content),
                    ImagePath = post.ImagePath,
                    AuthorName = GetDisplayName(post.Author, post.IsAnonymous),
                    IsAnonymous = post.IsAnonymous,
                    IsAuthorModerator = !post.IsAnonymous && moderatorIds.Contains(post.AuthorId),
                    IsLocked = post.IsLocked,
                    IsPinned = post.IsPinned,
                    CreatedAt = post.CreatedAt,
                    CommentCount = post.Comments.Count(comment => !comment.IsDeleted),
                    LikeCount = post.Likes.Count,
                    FavoriteCount = post.Favorites.Count,
                    IsLikedByCurrentUser = currentUserId != null && post.Likes.Any(like => like.UserId == currentUserId),
                    IsFavoritedByCurrentUser = currentUserId != null && post.Favorites.Any(favorite => favorite.UserId == currentUserId),
                    Tags = post.PostTags
                        .Select(postTag => postTag.ForumTag.Name)
                        .OrderBy(name => name)
                        .ToList()
                }).ToList()
            };

            return View(model);
        }

        [Authorize]
        public async Task<IActionResult> Saved()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Challenge();
            }

            var favorites = await _context.ForumPostFavorites
                .AsNoTracking()
                .AsSplitQuery()
                .Where(favorite => favorite.UserId == userId && !favorite.ForumPost.IsDeleted)
                .OrderByDescending(favorite => favorite.CreatedAt)
                .Include(favorite => favorite.ForumPost)
                    .ThenInclude(post => post.Author)
                .Include(favorite => favorite.ForumPost)
                    .ThenInclude(post => post.PostTags)
                        .ThenInclude(postTag => postTag.ForumTag)
                .Include(favorite => favorite.ForumPost)
                    .ThenInclude(post => post.Comments)
                .Include(favorite => favorite.ForumPost)
                    .ThenInclude(post => post.Likes)
                .Include(favorite => favorite.ForumPost)
                    .ThenInclude(post => post.Favorites)
                .ToListAsync();

            var moderatorIds = await GetModeratorIdsAsync(
                favorites.Select(favorite => favorite.ForumPost.AuthorId));

            var model = new SavedDiscussionsViewModel
            {
                Posts = favorites.Select(favorite =>
                {
                    var post = favorite.ForumPost;
                    return new ForumPostSummaryViewModel
                    {
                        Id = post.Id,
                        Title = post.Title,
                        Preview = BuildPreview(post.Content),
                        ImagePath = post.ImagePath,
                        AuthorName = GetDisplayName(post.Author, post.IsAnonymous),
                        IsAnonymous = post.IsAnonymous,
                        IsAuthorModerator = !post.IsAnonymous && moderatorIds.Contains(post.AuthorId),
                        IsLocked = post.IsLocked,
                        IsPinned = post.IsPinned,
                        CreatedAt = post.CreatedAt,
                        SavedAt = favorite.CreatedAt,
                        CommentCount = post.Comments.Count(comment => !comment.IsDeleted),
                        LikeCount = post.Likes.Count,
                        FavoriteCount = post.Favorites.Count,
                        IsLikedByCurrentUser = post.Likes.Any(like => like.UserId == userId),
                        IsFavoritedByCurrentUser = true,
                        Tags = post.PostTags
                            .Select(postTag => postTag.ForumTag.Name)
                            .OrderBy(name => name)
                            .ToList()
                    };
                }).ToList()
            };

            return View(model);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var currentUserId = _userManager.GetUserId(User);
            var post = await _context.ForumPosts
                .AsNoTracking()
                .AsSplitQuery()
                .Include(item => item.Author)
                .Include(item => item.PostTags)
                    .ThenInclude(postTag => postTag.ForumTag)
                .Include(item => item.Comments)
                    .ThenInclude(comment => comment.Author)
                .Include(item => item.Likes)
                .Include(item => item.Favorites)
                .FirstOrDefaultAsync(item => item.Id == id && !item.IsDeleted);

            if (post == null)
            {
                return NotFound();
            }

            var moderatorIds = await GetModeratorIdsAsync(
                post.Comments.Select(comment => comment.AuthorId).Append(post.AuthorId));

            var model = new ForumPostDetailsViewModel
            {
                Id = post.Id,
                Title = post.Title,
                Content = post.Content,
                ImagePath = post.ImagePath,
                AuthorName = GetDisplayName(post.Author, post.IsAnonymous),
                IsAnonymous = post.IsAnonymous,
                IsAuthorModerator = !post.IsAnonymous && moderatorIds.Contains(post.AuthorId),
                IsLocked = post.IsLocked,
                IsPinned = post.IsPinned,
                CreatedAt = post.CreatedAt,
                UpdatedAt = post.UpdatedAt,
                LikeCount = post.Likes.Count,
                FavoriteCount = post.Favorites.Count,
                IsLikedByCurrentUser = currentUserId != null && post.Likes.Any(like => like.UserId == currentUserId),
                IsFavoritedByCurrentUser = currentUserId != null && post.Favorites.Any(favorite => favorite.UserId == currentUserId),
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
                        IsAuthorModerator = moderatorIds.Contains(comment.AuthorId),
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

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLike(int postId, string? returnUrl)
        {
            if (!await _context.ForumPosts.AnyAsync(post => post.Id == postId && !post.IsDeleted))
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Challenge();
            }

            var existingLike = await _context.ForumPostLikes.FindAsync(postId, userId);
            if (existingLike == null)
            {
                _context.ForumPostLikes.Add(new ForumPostLike
                {
                    ForumPostId = postId,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                });
                TempData["ForumMessage"] = "Discussion liked.";
            }
            else
            {
                _context.ForumPostLikes.Remove(existingLike);
                TempData["ForumMessage"] = "Like removed.";
            }

            await _context.SaveChangesAsync();
            return RedirectToForumLocation(postId, returnUrl);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFavorite(int postId, string? returnUrl)
        {
            if (!await _context.ForumPosts.AnyAsync(post => post.Id == postId && !post.IsDeleted))
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Challenge();
            }

            var existingFavorite = await _context.ForumPostFavorites.FindAsync(postId, userId);
            if (existingFavorite == null)
            {
                _context.ForumPostFavorites.Add(new ForumPostFavorite
                {
                    ForumPostId = postId,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                });
                TempData["ForumMessage"] = "Discussion saved.";
            }
            else
            {
                _context.ForumPostFavorites.Remove(existingFavorite);
                TempData["ForumMessage"] = "Discussion removed from saved items.";
            }

            await _context.SaveChangesAsync();
            return RedirectToForumLocation(postId, returnUrl);
        }

        [Authorize]
        public IActionResult Create()
        {
            return View(new CreateForumPostViewModel());
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(PostImageStorage.MaxFileSizeBytes + 1_048_576)]
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

            string? imagePath = null;

            if (model.Image != null)
            {
                var imageResult = await _postImageStorage.SaveAsync(
                    model.Image,
                    HttpContext.RequestAborted);

                if (!imageResult.IsSuccess)
                {
                    ModelState.AddModelError(nameof(model.Image), imageResult.ErrorMessage!);
                    return View(model);
                }

                imagePath = imageResult.ImagePath;
            }

            try
            {
                var post = new ForumPost
                {
                    Title = model.Title.Trim(),
                    Content = model.Content.Trim(),
                    ImagePath = imagePath,
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
            catch
            {
                if (imagePath != null)
                {
                    await _postImageStorage.DeleteAsync(imagePath);
                }

                throw;
            }
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
                return RedirectToAction(nameof(Details), null, new { id = post.Id }, "add-comment");
            }

            if (!ModelState.IsValid)
            {
                TempData["ForumError"] = "Please write a valid comment before submitting.";
                return RedirectToAction(nameof(Details), null, new { id = post.Id }, "add-comment");
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
            return RedirectToAction(nameof(Details), null, new { id = post.Id }, "comments");
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
                    ForumReportStatuses.Active.Contains(report.Status) &&
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
                Status = ForumReportStatuses.Open,
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

        private async Task<HashSet<string>> GetModeratorIdsAsync(IEnumerable<string> userIds)
        {
            var ids = userIds.Distinct().ToList();
            if (ids.Count == 0)
            {
                return new HashSet<string>();
            }

            var normalizedRoleName = _userManager.NormalizeName(UserRoles.Moderator);
            var moderatorIds = await (
                from userRole in _context.UserRoles
                join role in _context.Roles on userRole.RoleId equals role.Id
                where ids.Contains(userRole.UserId) && role.NormalizedName == normalizedRoleName
                select userRole.UserId)
                .ToListAsync();

            return moderatorIds.ToHashSet();
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

        private IActionResult RedirectToForumLocation(int postId, string? returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction(nameof(Details), new { id = postId });
        }
    }
}
