using AgroForum.Data;
using AgroForum.Models.Community;
using AgroForum.Models.Forum;
using Microsoft.EntityFrameworkCore;

namespace AgroForum.Services.Community;

public sealed class CommunityNotificationService
{
    private readonly ApplicationDbContext _context;

    public CommunityNotificationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task QueuePostLikedAsync(
        int postId,
        string recipientId,
        string actorId,
        CancellationToken cancellationToken)
    {
        if (recipientId == actorId)
        {
            return;
        }

        var exists = await _context.UserNotifications.AnyAsync(notification =>
            notification.UserId == recipientId &&
            notification.ActorId == actorId &&
            notification.ForumPostId == postId &&
            notification.Type == NotificationTypes.PostLiked,
            cancellationToken);

        if (!exists)
        {
            _context.UserNotifications.Add(new UserNotification
            {
                UserId = recipientId,
                ActorId = actorId,
                ForumPostId = postId,
                Type = NotificationTypes.PostLiked,
                CreatedAt = DateTime.UtcNow
            });
        }
    }

    public async Task RemovePostLikedAsync(
        int postId,
        string recipientId,
        string actorId,
        CancellationToken cancellationToken)
    {
        var notification = await _context.UserNotifications.FirstOrDefaultAsync(item =>
            item.UserId == recipientId &&
            item.ActorId == actorId &&
            item.ForumPostId == postId &&
            item.Type == NotificationTypes.PostLiked,
            cancellationToken);

        if (notification != null)
        {
            _context.UserNotifications.Remove(notification);
        }
    }

    public void QueuePostCommented(ForumPost post, ForumComment comment, string actorId)
    {
        if (post.AuthorId == actorId)
        {
            return;
        }

        _context.UserNotifications.Add(new UserNotification
        {
            UserId = post.AuthorId,
            ActorId = actorId,
            ForumPostId = post.Id,
            ForumComment = comment,
            Type = NotificationTypes.PostCommented,
            CreatedAt = DateTime.UtcNow
        });
    }

    public async Task QueueSolutionAcceptedAsync(
        ForumPost post,
        ForumComment comment,
        string actorId,
        CancellationToken cancellationToken)
    {
        if (comment.AuthorId == actorId)
        {
            return;
        }

        var exists = await _context.UserNotifications.AnyAsync(notification =>
            notification.UserId == comment.AuthorId &&
            notification.ActorId == actorId &&
            notification.ForumPostId == post.Id &&
            notification.ForumCommentId == comment.Id &&
            notification.Type == NotificationTypes.SolutionAccepted,
            cancellationToken);

        if (!exists)
        {
            _context.UserNotifications.Add(new UserNotification
            {
                UserId = comment.AuthorId,
                ActorId = actorId,
                ForumPostId = post.Id,
                ForumCommentId = comment.Id,
                Type = NotificationTypes.SolutionAccepted,
                CreatedAt = DateTime.UtcNow
            });
        }
    }

    public async Task RemoveSolutionAcceptedAsync(
        int postId,
        int commentId,
        CancellationToken cancellationToken)
    {
        var notifications = await _context.UserNotifications
            .Where(notification =>
                notification.ForumPostId == postId &&
                notification.ForumCommentId == commentId &&
                notification.Type == NotificationTypes.SolutionAccepted)
            .ToListAsync(cancellationToken);

        if (notifications.Count > 0)
        {
            _context.UserNotifications.RemoveRange(notifications);
        }
    }
}
