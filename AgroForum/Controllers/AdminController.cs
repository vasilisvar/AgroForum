using System.Text.RegularExpressions;
using AgroForum.Constants;
using AgroForum.Data;
using AgroForum.Models;
using AgroForum.Models.Forum;
using AgroForum.Models.Moderation;
using AgroForum.ViewModels.Admin;
using AgroForum.ViewModels.Forum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgroForum.Controllers
{
    [Authorize(Roles = UserRoles.Admin)]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string search = "")
        {
            var moderatorIds = await GetUserIdsInRoleAsync(UserRoles.Moderator);
            var adminIds = await GetUserIdsInRoleAsync(UserRoles.Admin);

            var usersQuery = _context.Users.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                usersQuery = usersQuery.Where(user =>
                    (user.Email != null && user.Email.Contains(term)) ||
                    (user.UserName != null && user.UserName.Contains(term)) ||
                    (user.FirstName != null && user.FirstName.Contains(term)) ||
                    (user.LastName != null && user.LastName.Contains(term)));
            }

            var users = await usersQuery
                .OrderBy(user => user.Email)
                .Take(50)
                .ToListAsync();

            var moderators = await _context.Users
                .AsNoTracking()
                .Where(user => moderatorIds.Contains(user.Id))
                .OrderBy(user => user.Email)
                .ToListAsync();

            var moderatorTicketStats = await _context.ForumReports
                .AsNoTracking()
                .Where(ticket => ticket.AssignedToId != null && moderatorIds.Contains(ticket.AssignedToId))
                .GroupBy(ticket => ticket.AssignedToId!)
                .Select(group => new
                {
                    UserId = group.Key,
                    Active = group.Count(ticket => ForumReportStatuses.Active.Contains(ticket.Status)),
                    Resolved = group.Count(ticket => ticket.Status == ForumReportStatuses.Resolved || ticket.Status == ForumReportStatuses.Dismissed)
                })
                .ToDictionaryAsync(item => item.UserId);

            var moderatorActionStats = await _context.ModerationActions
                .AsNoTracking()
                .Where(action => moderatorIds.Contains(action.ActorId))
                .GroupBy(action => action.ActorId)
                .Select(group => new
                {
                    UserId = group.Key,
                    Count = group.Count(),
                    LastActiveAt = group.Max(action => action.CreatedAt)
                })
                .ToDictionaryAsync(item => item.UserId);

            var ticketEntities = await _context.ForumReports
                .AsNoTracking()
                .Include(ticket => ticket.AssignedTo)
                .Include(ticket => ticket.ForumPost)
                .Include(ticket => ticket.ForumComment)
                    .ThenInclude(comment => comment!.ForumPost)
                .Where(ticket => ForumReportStatuses.Active.Contains(ticket.Status))
                .OrderByDescending(ticket => ticket.CreatedAt)
                .Take(25)
                .ToListAsync();

            var completedTicketEntities = await _context.ForumReports
                .AsNoTracking()
                .Include(ticket => ticket.AssignedTo)
                .Include(ticket => ticket.ForumPost)
                .Include(ticket => ticket.ForumComment)
                    .ThenInclude(comment => comment!.ForumPost)
                .Where(ticket => ticket.Status == ForumReportStatuses.Resolved || ticket.Status == ForumReportStatuses.Dismissed)
                .OrderByDescending(ticket => ticket.ReviewedAt)
                .Take(15)
                .ToListAsync();

            var postEntities = await _context.ForumPosts
                .AsNoTracking()
                .Include(post => post.Author)
                .Where(post => !post.IsDeleted)
                .OrderByDescending(post => post.CreatedAt)
                .Take(20)
                .ToListAsync();

            var deletedPosts = await _context.ForumPosts
                .AsNoTracking()
                .Where(post => post.IsDeleted)
                .OrderByDescending(post => post.DeletedAt)
                .Take(15)
                .Select(post => new DeletedContentViewModel
                {
                    TargetType = "Post",
                    TargetId = post.Id,
                    ForumPostId = post.Id,
                    TargetDisplay = post.Title,
                    DeletionReason = post.DeletionReason,
                    DeletedAt = post.DeletedAt
                })
                .ToListAsync();

            var deletedComments = await _context.ForumComments
                .AsNoTracking()
                .Include(comment => comment.ForumPost)
                .Where(comment => comment.IsDeleted && !comment.ForumPost.IsDeleted)
                .OrderByDescending(comment => comment.DeletedAt)
                .Take(15)
                .Select(comment => new DeletedContentViewModel
                {
                    TargetType = "Comment",
                    TargetId = comment.Id,
                    ForumPostId = comment.ForumPostId,
                    TargetDisplay = comment.ForumPost.Title + ": " + comment.Content,
                    DeletionReason = comment.DeletionReason,
                    DeletedAt = comment.DeletedAt
                })
                .ToListAsync();

            var actionEntities = await _context.ModerationActions
                .AsNoTracking()
                .Include(action => action.Actor)
                .OrderByDescending(action => action.CreatedAt)
                .Take(50)
                .ToListAsync();

            var model = new AdminDashboardViewModel
            {
                TotalUsers = await _context.Users.CountAsync(),
                ModeratorCount = moderatorIds.Count,
                ActiveTicketCount = await _context.ForumReports.CountAsync(ticket => ForumReportStatuses.Active.Contains(ticket.Status)),
                DeletedContentCount = await _context.ForumPosts.CountAsync(post => post.IsDeleted) +
                    await _context.ForumComments.CountAsync(comment => comment.IsDeleted),
                Search = search,
                Users = users.Select(user => new AdminUserViewModel
                {
                    Id = user.Id,
                    DisplayName = DisplayName(user),
                    Email = user.Email ?? user.UserName ?? "No email",
                    CreatedAt = user.CreatedAt,
                    IsModerator = moderatorIds.Contains(user.Id),
                    IsAdmin = adminIds.Contains(user.Id)
                }).ToList(),
                Moderators = moderators.Select(user =>
                {
                    moderatorTicketStats.TryGetValue(user.Id, out var ticketStats);
                    moderatorActionStats.TryGetValue(user.Id, out var actionStats);
                    return new ModeratorSummaryViewModel
                    {
                        Id = user.Id,
                        DisplayName = DisplayName(user),
                        Email = user.Email ?? user.UserName ?? "No email",
                        ActiveTicketCount = ticketStats?.Active ?? 0,
                        ResolvedTicketCount = ticketStats?.Resolved ?? 0,
                        ActionCount = actionStats?.Count ?? 0,
                        LastActiveAt = actionStats?.LastActiveAt
                    };
                }).ToList(),
                ActiveTickets = ticketEntities.Select(ticket => new AdminTicketViewModel
                {
                    Id = ticket.Id,
                    TargetType = ticket.ForumCommentId.HasValue ? "Comment" : "Post",
                    TargetTitle = ticket.ForumPost?.Title ?? ticket.ForumComment?.ForumPost.Title ?? "Unknown post",
                    Reason = ticket.Reason,
                    Status = ticket.Status,
                    AssignedModeratorName = ticket.AssignedTo == null ? "Unassigned" : DisplayName(ticket.AssignedTo),
                    AssignedToId = ticket.AssignedToId,
                    CreatedAt = ticket.CreatedAt
                }).ToList(),
                RecentCompletedTickets = completedTicketEntities.Select(ticket => new AdminTicketViewModel
                {
                    Id = ticket.Id,
                    TargetType = ticket.ForumCommentId.HasValue ? "Comment" : "Post",
                    TargetTitle = ticket.ForumPost?.Title ?? ticket.ForumComment?.ForumPost.Title ?? "Unknown post",
                    Reason = ticket.Reason,
                    Status = ticket.Status,
                    AssignedModeratorName = ticket.AssignedTo == null ? "Unassigned" : DisplayName(ticket.AssignedTo),
                    AssignedToId = ticket.AssignedToId,
                    CreatedAt = ticket.CreatedAt
                }).ToList(),
                RecentPosts = postEntities.Select(post => new AdminContentViewModel
                {
                    Id = post.Id,
                    Title = post.Title,
                    AuthorName = post.IsAnonymous ? "Anonymous farmer" : DisplayName(post.Author),
                    IsPinned = post.IsPinned,
                    IsLocked = post.IsLocked,
                    CreatedAt = post.CreatedAt
                }).ToList(),
                DeletedContent = deletedPosts
                    .Concat(deletedComments)
                    .OrderByDescending(item => item.DeletedAt)
                    .Take(20)
                    .Select(item =>
                    {
                        item.TargetDisplay = BuildPreview(item.TargetDisplay);
                        return item;
                    })
                    .ToList(),
                Activity = actionEntities.Select(action => new ModerationActivityViewModel
                {
                    Id = action.Id,
                    ActorName = DisplayName(action.Actor),
                    ActionType = action.ActionType,
                    TargetType = action.TargetType,
                    TargetDisplay = action.TargetDisplay,
                    Details = action.Details,
                    ForumReportId = action.ForumReportId,
                    CreatedAt = action.CreatedAt
                }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GrantModerator(string userId, string reason)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                return WithError("A role-change reason is required.");
            }

            if (!await _userManager.IsInRoleAsync(user, UserRoles.Moderator))
            {
                var result = await _userManager.AddToRoleAsync(user, UserRoles.Moderator);
                if (!result.Succeeded)
                {
                    return WithError(string.Join(" ", result.Errors.Select(error => error.Description)));
                }

                await _userManager.UpdateSecurityStampAsync(user);

                AddAction(ModerationActionTypes.ModeratorGranted, "User", user.Id, DisplayName(user), reason.Trim());
                await _context.SaveChangesAsync();
            }

            return WithSuccess("Moderator role granted. The badge is active now; board access begins on the user's next sign-in.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RevokeModerator(string userId, string reason)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                return WithError("A role-change reason is required.");
            }

            if (await _userManager.IsInRoleAsync(user, UserRoles.Moderator))
            {
                var activeTickets = await _context.ForumReports
                    .Where(ticket => ticket.AssignedToId == user.Id && ForumReportStatuses.Active.Contains(ticket.Status))
                    .ToListAsync();

                foreach (var ticket in activeTickets)
                {
                    ticket.Status = ForumReportStatuses.Open;
                    ticket.AssignedToId = null;
                    ticket.AssignedAt = null;
                }

                var result = await _userManager.RemoveFromRoleAsync(user, UserRoles.Moderator);
                if (!result.Succeeded)
                {
                    return WithError(string.Join(" ", result.Errors.Select(error => error.Description)));
                }

                await _userManager.UpdateSecurityStampAsync(user);

                AddAction(ModerationActionTypes.ModeratorRevoked, "User", user.Id, DisplayName(user), reason.Trim());
                await _context.SaveChangesAsync();
            }

            return WithSuccess("Moderator role revoked and active tickets returned to the open queue.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReassignTicket(int reportId, string moderatorId, string reason)
        {
            var report = await _context.ForumReports.FindAsync(reportId);
            var moderator = await _userManager.FindByIdAsync(moderatorId);
            if (report == null || moderator == null)
            {
                return NotFound();
            }

            if (!ForumReportStatuses.Active.Contains(report.Status))
            {
                return WithError("Completed tickets must be reopened before assignment.");
            }

            if (!await _userManager.IsInRoleAsync(moderator, UserRoles.Moderator))
            {
                return WithError("Tickets can only be assigned to an active moderator.");
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                return WithError("An assignment reason is required.");
            }

            report.Status = ForumReportStatuses.InReview;
            report.AssignedToId = moderator.Id;
            report.AssignedAt = DateTime.UtcNow;
            AddAction(ModerationActionTypes.TicketReassigned, "Ticket", report.Id.ToString(), $"Ticket #{report.Id}", $"Assigned to {DisplayName(moderator)}. {reason.Trim()}", report.Id);
            await _context.SaveChangesAsync();

            return WithSuccess("Ticket assignment updated.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReopenTicket(int reportId, string reason)
        {
            var report = await _context.ForumReports.FindAsync(reportId);
            if (report == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                return WithError("A reopen reason is required.");
            }

            report.Status = ForumReportStatuses.Open;
            report.AssignedToId = null;
            report.AssignedAt = null;
            report.ReviewedAt = null;
            report.ReviewedById = null;
            report.ModeratorNotes = null;
            AddAction(ModerationActionTypes.TicketReopened, "Ticket", report.Id.ToString(), $"Ticket #{report.Id}", reason.Trim(), report.Id);
            await _context.SaveChangesAsync();

            return WithSuccess("Ticket reopened and returned to the queue.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPinned(int postId, bool isPinned, string reason)
        {
            var post = await _context.ForumPosts.FindAsync(postId);
            if (post == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                return WithError("A feed-change reason is required.");
            }

            post.IsPinned = isPinned;
            AddAction(isPinned ? ModerationActionTypes.PostPinned : ModerationActionTypes.PostUnpinned, "Post", post.Id.ToString(), post.Title, reason.Trim());
            await _context.SaveChangesAsync();
            return WithSuccess(isPinned ? "Post pinned to the top of the feed." : "Post removed from the pinned feed.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPostLock(int postId, bool isLocked, string reason)
        {
            var post = await _context.ForumPosts.FindAsync(postId);
            if (post == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                return WithError("An action reason is required.");
            }

            post.IsLocked = isLocked;
            AddAction(isLocked ? ModerationActionTypes.PostLocked : ModerationActionTypes.PostUnlocked, "Post", post.Id.ToString(), post.Title, reason.Trim());
            await _context.SaveChangesAsync();
            return WithSuccess(isLocked ? "Discussion locked." : "Discussion unlocked.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemovePost(int postId, string reason)
        {
            var post = await _context.ForumPosts.FindAsync(postId);
            if (post == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                return WithError("A deletion reason is required.");
            }

            var actorId = CurrentUserId();
            post.IsDeleted = true;
            post.DeletedAt = DateTime.UtcNow;
            post.DeletedByUserId = actorId;
            post.DeletionReason = reason.Trim();
            AddAction(ModerationActionTypes.PostRemoved, "Post", post.Id.ToString(), post.Title, reason.Trim());
            await _context.SaveChangesAsync();
            return WithSuccess("Post soft-deleted. It remains available for restoration and audit.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestorePost(int postId, string reason)
        {
            var post = await _context.ForumPosts.FindAsync(postId);
            if (post == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                return WithError("A restoration reason is required.");
            }

            post.IsDeleted = false;
            post.DeletedAt = null;
            post.DeletedByUserId = null;
            post.DeletionReason = null;
            AddAction(ModerationActionTypes.PostRestored, "Post", post.Id.ToString(), post.Title, reason.Trim());
            await _context.SaveChangesAsync();
            return WithSuccess("Post restored to the forum.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreComment(int commentId, string reason)
        {
            var comment = await _context.ForumComments
                .Include(item => item.ForumPost)
                .FirstOrDefaultAsync(item => item.Id == commentId);
            if (comment == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                return WithError("A restoration reason is required.");
            }

            comment.IsDeleted = false;
            comment.DeletedAt = null;
            comment.DeletedByUserId = null;
            comment.DeletionReason = null;
            AddAction(ModerationActionTypes.CommentRestored, "Comment", comment.Id.ToString(), $"Comment in {comment.ForumPost.Title}", reason.Trim());
            await _context.SaveChangesAsync();
            return WithSuccess("Comment restored to the discussion.");
        }

        private async Task<HashSet<string>> GetUserIdsInRoleAsync(string roleName)
        {
            var normalizedName = _userManager.NormalizeName(roleName);
            var userIds = await (
                from userRole in _context.UserRoles
                join role in _context.Roles on userRole.RoleId equals role.Id
                where role.NormalizedName == normalizedName
                select userRole.UserId)
                .ToListAsync();

            return userIds.ToHashSet();
        }

        private void AddAction(string actionType, string targetType, string? targetId, string? targetDisplay, string details, int? reportId = null)
        {
            _context.ModerationActions.Add(new ModerationAction
            {
                ActorId = CurrentUserId(),
                ActionType = actionType,
                TargetType = targetType,
                TargetId = targetId,
                TargetDisplay = targetDisplay,
                Details = details,
                ForumReportId = reportId,
                CreatedAt = DateTime.UtcNow
            });
        }

        private string CurrentUserId()
        {
            return _userManager.GetUserId(User) ?? throw new InvalidOperationException("An authenticated administrator is required.");
        }

        private IActionResult WithSuccess(string message)
        {
            TempData["AdminMessage"] = message;
            return RedirectToAction(nameof(Index));
        }

        private IActionResult WithError(string message)
        {
            TempData["AdminError"] = message;
            return RedirectToAction(nameof(Index));
        }

        private static string DisplayName(ApplicationUser user)
        {
            var fullName = $"{user.FirstName} {user.LastName}".Trim();
            return string.IsNullOrWhiteSpace(fullName) ? user.UserName ?? "Community member" : fullName;
        }

        private static string BuildPreview(string content)
        {
            var preview = Regex.Replace(content, "\\s+", " ").Trim();
            return preview.Length <= 100 ? preview : $"{preview[..100]}...";
        }
    }
}
