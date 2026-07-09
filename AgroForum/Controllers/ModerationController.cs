using System.Text.RegularExpressions;
using AgroForum.Constants;
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
    [Authorize(Roles = UserRoles.Admin + "," + UserRoles.Moderator)]
    public class ModerationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ModerationController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var reports = await _context.ForumReports
                .AsNoTracking()
                .Include(report => report.Reporter)
                .Include(report => report.ForumPost)
                    .ThenInclude(post => post!.Author)
                .Include(report => report.ForumComment)
                    .ThenInclude(comment => comment!.Author)
                .Include(report => report.ForumComment)
                    .ThenInclude(comment => comment!.ForumPost)
                .OrderByDescending(report => report.CreatedAt)
                .ToListAsync();

            var mappedReports = reports.Select(MapReport).ToList();

            var model = new ModerationDashboardViewModel
            {
                PendingReports = mappedReports
                    .Where(report => report.Status == ForumReportStatuses.Pending)
                    .ToList(),
                ResolvedReports = mappedReports
                    .Where(report => report.Status != ForumReportStatuses.Pending)
                    .Take(20)
                    .ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DismissReport(int reportId, string? moderatorNotes)
        {
            var report = await _context.ForumReports.FindAsync(reportId);
            if (report == null)
            {
                return NotFound();
            }

            report.Status = ForumReportStatuses.Rejected;
            report.ReviewedAt = DateTime.UtcNow;
            report.ReviewedById = _userManager.GetUserId(User);
            report.ModeratorNotes = moderatorNotes?.Trim();

            await _context.SaveChangesAsync();

            TempData["ModerationMessage"] = "Report dismissed.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveComment(int reportId, string deletionReason)
        {
            var report = await _context.ForumReports
                .Include(item => item.ForumComment)
                .FirstOrDefaultAsync(item => item.Id == reportId);

            if (report?.ForumComment == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Challenge();
            }

            var reason = string.IsNullOrWhiteSpace(deletionReason)
                ? "Removed after moderator review."
                : deletionReason.Trim();

            report.ForumComment.IsDeleted = true;
            report.ForumComment.DeletedAt = DateTime.UtcNow;
            report.ForumComment.DeletedByUserId = userId;
            report.ForumComment.DeletionReason = reason;

            var relatedReports = await _context.ForumReports
                .Where(item =>
                    item.ForumCommentId == report.ForumCommentId &&
                    item.Status == ForumReportStatuses.Pending)
                .ToListAsync();

            foreach (var relatedReport in relatedReports)
            {
                relatedReport.Status = ForumReportStatuses.Accepted;
                relatedReport.ReviewedAt = DateTime.UtcNow;
                relatedReport.ReviewedById = userId;
                relatedReport.ModeratorNotes = reason;
            }

            await _context.SaveChangesAsync();

            TempData["ModerationMessage"] = "Comment removed and report marked as accepted.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPostTag(int postId, string tagName)
        {
            var post = await _context.ForumPosts
                .Include(item => item.PostTags)
                    .ThenInclude(postTag => postTag.ForumTag)
                .FirstOrDefaultAsync(item => item.Id == postId && !item.IsDeleted);

            if (post == null)
            {
                return NotFound();
            }

            var parsedTag = ForumTagUtilities.ParseTagNames(tagName).FirstOrDefault();
            if (parsedTag == null)
            {
                TempData["ForumError"] = "Write a tag name before adding it.";
                return RedirectToAction("Details", "Forum", new { id = post.Id });
            }

            var slug = ForumTagUtilities.CreateSlug(parsedTag);
            var tag = await _context.ForumTags.FirstOrDefaultAsync(item => item.Slug == slug);

            if (tag == null)
            {
                tag = new ForumTag
                {
                    Name = parsedTag,
                    Slug = slug
                };
                _context.ForumTags.Add(tag);
            }

            var alreadyTagged = post.PostTags.Any(postTag => postTag.ForumTag.Slug == slug);
            if (!alreadyTagged)
            {
                post.PostTags.Add(new ForumPostTag
                {
                    ForumPost = post,
                    ForumTag = tag
                });

                await _context.SaveChangesAsync();
                TempData["ForumMessage"] = "Tag added by moderator.";
            }

            return RedirectToAction("Details", "Forum", new { id = post.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemovePostTag(int postId, int tagId)
        {
            var postTag = await _context.ForumPostTags
                .FirstOrDefaultAsync(item => item.ForumPostId == postId && item.ForumTagId == tagId);

            if (postTag == null)
            {
                return NotFound();
            }

            _context.ForumPostTags.Remove(postTag);
            await _context.SaveChangesAsync();

            TempData["ForumMessage"] = "Tag removed by moderator.";
            return RedirectToAction("Details", "Forum", new { id = postId });
        }

        private static ModerationReportViewModel MapReport(ForumReport report)
        {
            var isCommentReport = report.ForumCommentId.HasValue;
            var targetPost = isCommentReport ? report.ForumComment?.ForumPost : report.ForumPost;
            var targetContent = isCommentReport ? report.ForumComment?.Content : report.ForumPost?.Content;
            var targetAuthor = isCommentReport ? report.ForumComment?.Author : report.ForumPost?.Author;
            var isDeleted = isCommentReport
                ? report.ForumComment?.IsDeleted ?? false
                : report.ForumPost?.IsDeleted ?? false;

            return new ModerationReportViewModel
            {
                Id = report.Id,
                TargetType = isCommentReport ? "Comment" : "Post",
                ForumPostId = targetPost?.Id,
                ForumCommentId = report.ForumCommentId,
                TargetTitle = targetPost?.Title ?? "Unknown post",
                TargetContent = BuildPreview(targetContent ?? string.Empty),
                TargetAuthorName = GetDisplayName(targetAuthor, isAnonymous: false),
                ReporterName = GetDisplayName(report.Reporter, isAnonymous: false),
                Reason = report.Reason,
                Details = report.Details,
                Status = report.Status,
                CreatedAt = report.CreatedAt,
                IsTargetDeleted = isDeleted
            };
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
            return preview.Length <= 220 ? preview : $"{preview[..220]}...";
        }
    }
}
