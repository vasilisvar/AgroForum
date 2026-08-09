using AgroForum.Data;
using AgroForum.Helpers;
using AgroForum.Models;
using AgroForum.Models.Community;
using AgroForum.ViewModels.Community;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgroForum.Controllers;

[Authorize]
public sealed class NotificationsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public NotificationsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
        {
            return Challenge();
        }

        var unreadCount = await _context.UserNotifications
            .AsNoTracking()
            .CountAsync(notification => notification.UserId == userId && notification.ReadAt == null);

        var notifications = await _context.UserNotifications
            .AsNoTracking()
            .Where(notification => notification.UserId == userId)
            .Include(notification => notification.Actor)
            .Include(notification => notification.ForumPost)
            .OrderByDescending(notification => notification.CreatedAt)
            .Take(50)
            .ToListAsync();

        var model = new NotificationCenterViewModel
        {
            UnreadCount = unreadCount,
            Notifications = notifications.Select(MapNotification).ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Open(long id)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
        {
            return Challenge();
        }

        var notification = await _context.UserNotifications
            .Include(item => item.ForumPost)
            .FirstOrDefaultAsync(item => item.Id == id && item.UserId == userId);

        if (notification == null)
        {
            return NotFound();
        }

        if (notification.ReadAt == null)
        {
            notification.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        if (notification.ForumPost is { IsDeleted: false } post)
        {
            var target = Url.Action("Details", "Forum", new { id = post.Id }) ?? $"/Forum/Details/{post.Id}";
            if (notification.ForumCommentId.HasValue)
            {
                target += $"#comment-{notification.ForumCommentId.Value}";
            }

            return LocalRedirect(target);
        }

        TempData["NotificationMessage"] = "That discussion is no longer available.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
        {
            return Challenge();
        }

        var unread = await _context.UserNotifications
            .Where(notification => notification.UserId == userId && notification.ReadAt == null)
            .ToListAsync();

        var readAt = DateTime.UtcNow;
        foreach (var notification in unread)
        {
            notification.ReadAt = readAt;
        }

        await _context.SaveChangesAsync();
        TempData["NotificationMessage"] = unread.Count == 0
            ? "You're already caught up."
            : "All notifications marked as read.";
        return RedirectToAction(nameof(Index));
    }

    private static NotificationItemViewModel MapNotification(UserNotification notification)
    {
        var actorName = CommunityDisplayName.For(notification.Actor);
        var postTitle = notification.ForumPost?.Title ?? "a discussion";
        var isComment = notification.Type == NotificationTypes.PostCommented;

        return new NotificationItemViewModel
        {
            Id = notification.Id,
            ActorId = notification.ActorId,
            ActorName = actorName,
            Message = isComment
                ? $"commented on “{postTitle}”"
                : $"liked “{postTitle}”",
            Type = isComment ? "New comment" : "New like",
            Symbol = isComment ? "💬" : "♥",
            CreatedAt = notification.CreatedAt,
            IsRead = notification.ReadAt != null,
            CanOpen = notification.ForumPost is { IsDeleted: false },
            ForumPostId = notification.ForumPostId,
            ForumCommentId = notification.ForumCommentId
        };
    }
}
