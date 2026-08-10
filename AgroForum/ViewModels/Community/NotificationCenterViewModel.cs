namespace AgroForum.ViewModels.Community;

public sealed class NotificationCenterViewModel
{
    public int UnreadCount { get; set; }

    public IReadOnlyList<NotificationItemViewModel> Notifications { get; set; } = Array.Empty<NotificationItemViewModel>();
}

public sealed class NotificationItemViewModel
{
    public long Id { get; set; }

    public string? ActorId { get; set; }

    public string ActorName { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string Symbol { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public bool IsRead { get; set; }

    public bool CanOpen { get; set; }

    public int? ForumPostId { get; set; }

    public int? ForumCommentId { get; set; }
}
