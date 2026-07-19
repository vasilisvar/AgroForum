using System.Text.RegularExpressions;
using AgroForum.Constants;
using AgroForum.Data;
using AgroForum.Helpers;
using AgroForum.Models;
using AgroForum.Models.Forum;
using AgroForum.Models.Moderation;
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

        public async Task<IActionResult> Index(
            string status = "Active",
            string targetType = "",
            string assignment = "All")
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Challenge();
            }

            var ticketQuery = _context.ForumReports
                .AsNoTracking()
                .Include(report => report.Reporter)
                .Include(report => report.AssignedTo)
                .Include(report => report.ReviewedBy)
                .Include(report => report.ForumPost)
                    .ThenInclude(post => post!.Author)
                .Include(report => report.ForumComment)
                    .ThenInclude(comment => comment!.Author)
                .Include(report => report.ForumComment)
                    .ThenInclude(comment => comment!.ForumPost)
                .AsQueryable();

            ticketQuery = status switch
            {
                ForumReportStatuses.Open => ticketQuery.Where(ticket => ticket.Status == ForumReportStatuses.Open),
                ForumReportStatuses.InReview => ticketQuery.Where(ticket => ticket.Status == ForumReportStatuses.InReview),
                ForumReportStatuses.Resolved => ticketQuery.Where(ticket => ticket.Status == ForumReportStatuses.Resolved),
                ForumReportStatuses.Dismissed => ticketQuery.Where(ticket => ticket.Status == ForumReportStatuses.Dismissed),
                _ => ticketQuery.Where(ticket => ForumReportStatuses.Active.Contains(ticket.Status))
            };

            if (targetType == "Post")
            {
                ticketQuery = ticketQuery.Where(ticket => ticket.ForumPostId != null);
            }
            else if (targetType == "Comment")
            {
                ticketQuery = ticketQuery.Where(ticket => ticket.ForumCommentId != null);
            }

            ticketQuery = assignment switch
            {
                "Mine" => ticketQuery.Where(ticket => ticket.AssignedToId == userId),
                "Unassigned" => ticketQuery.Where(ticket => ticket.AssignedToId == null),
                _ => ticketQuery
            };

            var tickets = await ticketQuery
                .OrderByDescending(ticket => ticket.CreatedAt)
                .Take(200)
                .ToListAsync();

            var recentDecisions = await _context.ForumReports
                .AsNoTracking()
                .Include(report => report.Reporter)
                .Include(report => report.AssignedTo)
                .Include(report => report.ReviewedBy)
                .Include(report => report.ForumPost)
                    .ThenInclude(post => post!.Author)
                .Include(report => report.ForumComment)
                    .ThenInclude(comment => comment!.Author)
                .Include(report => report.ForumComment)
                    .ThenInclude(comment => comment!.ForumPost)
                .Where(ticket => ticket.Status == ForumReportStatuses.Resolved || ticket.Status == ForumReportStatuses.Dismissed)
                .OrderByDescending(ticket => ticket.ReviewedAt)
                .Take(10)
                .ToListAsync();

            var recentActionEntities = await _context.ModerationActions
                .AsNoTracking()
                .Include(action => action.Actor)
                .Where(action => action.ActorId == userId)
                .OrderByDescending(action => action.CreatedAt)
                .Take(10)
                .ToListAsync();

            var recentActivity = recentActionEntities
                .Select(action => new ModerationActivityViewModel
                {
                    Id = action.Id,
                    ActorName = DisplayName(action.Actor),
                    ActionType = action.ActionType,
                    TargetType = action.TargetType,
                    TargetDisplay = action.TargetDisplay,
                    Details = action.Details,
                    ForumReportId = action.ForumReportId,
                    CreatedAt = action.CreatedAt
                })
                .ToList();

            var startOfTodayUtc = DateTime.UtcNow.Date;
            var model = new ModerationDashboardViewModel
            {
                OpenTicketCount = await _context.ForumReports.CountAsync(ticket => ForumReportStatuses.Active.Contains(ticket.Status)),
                MyTicketCount = await _context.ForumReports.CountAsync(ticket =>
                    ForumReportStatuses.Active.Contains(ticket.Status) && ticket.AssignedToId == userId),
                ResolvedTodayCount = await _context.ForumReports.CountAsync(ticket =>
                    (ticket.Status == ForumReportStatuses.Resolved || ticket.Status == ForumReportStatuses.Dismissed) &&
                    ticket.ReviewedAt >= startOfTodayUtc),
                StatusFilter = status,
                TargetTypeFilter = targetType,
                AssignmentFilter = assignment,
                Tickets = tickets.Select(ticket => MapReport(ticket, userId, User.IsInRole(UserRoles.Admin))).ToList(),
                RecentDecisions = recentDecisions.Select(ticket => MapReport(ticket, userId, User.IsInRole(UserRoles.Admin))).ToList(),
                RecentActivity = recentActivity
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClaimReport(int reportId)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Challenge();
            }

            var report = await _context.ForumReports.FirstOrDefaultAsync(ticket => ticket.Id == reportId);
            if (report == null)
            {
                return NotFound();
            }

            if (report.Status != ForumReportStatuses.Open || report.AssignedToId != null)
            {
                TempData["ModerationError"] = "This ticket has already been claimed or completed.";
                return RedirectToAction(nameof(Index));
            }

            report.Status = ForumReportStatuses.InReview;
            report.AssignedToId = userId;
            report.AssignedAt = DateTime.UtcNow;
            AddAction(userId, ModerationActionTypes.TicketClaimed, "Ticket", report.Id.ToString(), $"Ticket #{report.Id}", null, report.Id);

            return await SaveAndReturnAsync("Ticket claimed. It is now in your queue.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReleaseReport(int reportId)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Challenge();
            }

            var report = await _context.ForumReports.FirstOrDefaultAsync(ticket => ticket.Id == reportId);
            if (report == null)
            {
                return NotFound();
            }

            if (report.Status != ForumReportStatuses.InReview || report.AssignedToId != userId)
            {
                return Forbid();
            }

            report.Status = ForumReportStatuses.Open;
            report.AssignedToId = null;
            report.AssignedAt = null;
            AddAction(userId, ModerationActionTypes.TicketReleased, "Ticket", report.Id.ToString(), $"Ticket #{report.Id}", null, report.Id);

            return await SaveAndReturnAsync("Ticket returned to the open queue.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DismissReport(int reportId, string moderatorNotes)
        {
            var report = await _context.ForumReports.FindAsync(reportId);
            if (report == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Challenge();
            }

            if (!CanHandle(report, userId))
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(moderatorNotes))
            {
                TempData["ModerationError"] = "Add a decision note before dismissing a ticket.";
                return RedirectToAction(nameof(Index));
            }

            CompleteReport(report, userId, ForumReportStatuses.Dismissed, moderatorNotes.Trim());
            AddAction(userId, ModerationActionTypes.TicketDismissed, "Ticket", report.Id.ToString(), $"Ticket #{report.Id}", moderatorNotes.Trim(), report.Id);

            return await SaveAndReturnAsync("Ticket dismissed with an audit note.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveComment(int reportId, string deletionReason)
        {
            var report = await _context.ForumReports
                .Include(ticket => ticket.ForumComment)
                .FirstOrDefaultAsync(ticket => ticket.Id == reportId);

            if (report?.ForumComment == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Challenge();
            }

            if (!CanHandle(report, userId))
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(deletionReason))
            {
                TempData["ModerationError"] = "A deletion reason is required.";
                return RedirectToAction(nameof(Index));
            }

            var reason = deletionReason.Trim();
            report.ForumComment.IsDeleted = true;
            report.ForumComment.DeletedAt = DateTime.UtcNow;
            report.ForumComment.DeletedByUserId = userId;
            report.ForumComment.DeletionReason = reason;

            var relatedReports = await _context.ForumReports
                .Where(ticket => ticket.ForumCommentId == report.ForumCommentId && ForumReportStatuses.Active.Contains(ticket.Status))
                .ToListAsync();

            foreach (var relatedReport in relatedReports)
            {
                CompleteReport(relatedReport, userId, ForumReportStatuses.Resolved, reason);
            }

            AddAction(userId, ModerationActionTypes.CommentRemoved, "Comment", report.ForumCommentId.ToString(), $"Comment #{report.ForumCommentId}", reason, report.Id);
            return await SaveAndReturnAsync("Comment soft-deleted and related tickets resolved.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemovePost(int reportId, string deletionReason)
        {
            var report = await _context.ForumReports
                .Include(ticket => ticket.ForumPost)
                .FirstOrDefaultAsync(ticket => ticket.Id == reportId);

            if (report?.ForumPost == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Challenge();
            }

            if (!CanHandle(report, userId))
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(deletionReason))
            {
                TempData["ModerationError"] = "A deletion reason is required.";
                return RedirectToAction(nameof(Index));
            }

            var reason = deletionReason.Trim();
            report.ForumPost.IsDeleted = true;
            report.ForumPost.DeletedAt = DateTime.UtcNow;
            report.ForumPost.DeletedByUserId = userId;
            report.ForumPost.DeletionReason = reason;

            var relatedReports = await _context.ForumReports
                .Where(ticket => ForumReportStatuses.Active.Contains(ticket.Status) &&
                    (ticket.ForumPostId == report.ForumPostId ||
                     (ticket.ForumComment != null && ticket.ForumComment.ForumPostId == report.ForumPostId)))
                .ToListAsync();

            foreach (var relatedReport in relatedReports)
            {
                CompleteReport(relatedReport, userId, ForumReportStatuses.Resolved, reason);
            }

            AddAction(userId, ModerationActionTypes.PostRemoved, "Post", report.ForumPostId.ToString(), report.ForumPost.Title, reason, report.Id);
            return await SaveAndReturnAsync("Post soft-deleted and related tickets resolved.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPostLock(int reportId, bool isLocked, string reason)
        {
            var report = await _context.ForumReports
                .Include(ticket => ticket.ForumPost)
                .Include(ticket => ticket.ForumComment)
                    .ThenInclude(comment => comment!.ForumPost)
                .FirstOrDefaultAsync(ticket => ticket.Id == reportId);

            if (report == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Challenge();
            }

            if (!CanHandle(report, userId))
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["ModerationError"] = "An action reason is required.";
                return RedirectToAction(nameof(Index));
            }

            var post = report.ForumPost ?? report.ForumComment?.ForumPost;
            if (post == null)
            {
                return NotFound();
            }

            post.IsLocked = isLocked;
            CompleteReport(report, userId, ForumReportStatuses.Resolved, reason.Trim());
            AddAction(
                userId,
                isLocked ? ModerationActionTypes.PostLocked : ModerationActionTypes.PostUnlocked,
                "Post",
                post.Id.ToString(),
                post.Title,
                reason.Trim(),
                report.Id);

            return await SaveAndReturnAsync(isLocked ? "Discussion locked and ticket resolved." : "Discussion unlocked and ticket resolved.");
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
                tag = new ForumTag { Name = parsedTag, Slug = slug };
                _context.ForumTags.Add(tag);
            }

            if (post.PostTags.All(postTag => postTag.ForumTag.Slug != slug))
            {
                post.PostTags.Add(new ForumPostTag { ForumPost = post, ForumTag = tag });
                var userId = _userManager.GetUserId(User)!;
                AddAction(userId, ModerationActionTypes.TagAdded, "Post", post.Id.ToString(), post.Title, $"Added tag: {parsedTag}");
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
                .Include(item => item.ForumPost)
                .Include(item => item.ForumTag)
                .FirstOrDefaultAsync(item => item.ForumPostId == postId && item.ForumTagId == tagId);

            if (postTag == null)
            {
                return NotFound();
            }

            _context.ForumPostTags.Remove(postTag);
            var userId = _userManager.GetUserId(User)!;
            AddAction(userId, ModerationActionTypes.TagRemoved, "Post", postId.ToString(), postTag.ForumPost.Title, $"Removed tag: {postTag.ForumTag.Name}");
            await _context.SaveChangesAsync();

            TempData["ForumMessage"] = "Tag removed by moderator.";
            return RedirectToAction("Details", "Forum", new { id = postId });
        }

        private bool CanHandle(ForumReport report, string userId)
        {
            return User.IsInRole(UserRoles.Admin) ||
                (report.Status == ForumReportStatuses.InReview && report.AssignedToId == userId);
        }

        private static void CompleteReport(ForumReport report, string userId, string status, string notes)
        {
            report.Status = status;
            report.ReviewedAt = DateTime.UtcNow;
            report.ReviewedById = userId;
            report.ModeratorNotes = notes;
        }

        private void AddAction(
            string actorId,
            string actionType,
            string targetType,
            string? targetId,
            string? targetDisplay,
            string? details,
            int? reportId = null)
        {
            _context.ModerationActions.Add(new ModerationAction
            {
                ActorId = actorId,
                ActionType = actionType,
                TargetType = targetType,
                TargetId = targetId,
                TargetDisplay = targetDisplay,
                Details = details,
                ForumReportId = reportId,
                CreatedAt = DateTime.UtcNow
            });
        }

        private async Task<IActionResult> SaveAndReturnAsync(string successMessage)
        {
            try
            {
                await _context.SaveChangesAsync();
                TempData["ModerationMessage"] = successMessage;
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["ModerationError"] = "Another moderator changed this ticket. The queue has been refreshed.";
            }

            return RedirectToAction(nameof(Index));
        }

        private static ModerationReportViewModel MapReport(ForumReport report, string currentUserId, bool isAdmin)
        {
            var isCommentReport = report.ForumCommentId.HasValue;
            var targetPost = isCommentReport ? report.ForumComment?.ForumPost : report.ForumPost;
            var targetContent = isCommentReport ? report.ForumComment?.Content : report.ForumPost?.Content;
            var targetAuthor = isCommentReport ? report.ForumComment?.Author : report.ForumPost?.Author;
            var isAnonymous = !isCommentReport && (report.ForumPost?.IsAnonymous ?? false);
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
                TargetAuthorName = GetDisplayName(targetAuthor, isAnonymous),
                ReporterName = GetDisplayName(report.Reporter, false),
                Reason = report.Reason,
                Details = report.Details,
                Status = report.Status,
                CreatedAt = report.CreatedAt,
                AssignedAt = report.AssignedAt,
                ReviewedAt = report.ReviewedAt,
                AssignedModeratorName = report.AssignedTo == null ? "Unassigned" : GetDisplayName(report.AssignedTo, false),
                ReviewerName = report.ReviewedBy == null ? null : GetDisplayName(report.ReviewedBy, false),
                ModeratorNotes = report.ModeratorNotes,
                IsAssignedToCurrentUser = report.AssignedToId == currentUserId,
                CanHandle = isAdmin || (report.Status == ForumReportStatuses.InReview && report.AssignedToId == currentUserId),
                CanClaim = report.Status == ForumReportStatuses.Open && report.AssignedToId == null,
                IsPostLocked = targetPost?.IsLocked ?? false,
                IsTargetDeleted = isDeleted
            };
        }

        private static string GetDisplayName(ApplicationUser? user, bool isAnonymous)
        {
            if (isAnonymous)
            {
                return "Anonymous farmer";
            }

            return user == null ? "Community member" : DisplayName(user);
        }

        private static string DisplayName(ApplicationUser user)
        {
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
