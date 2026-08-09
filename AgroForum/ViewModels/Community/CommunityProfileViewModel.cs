namespace AgroForum.ViewModels.Community;

public sealed class CommunityProfileViewModel
{
    public string UserId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Initial { get; set; } = string.Empty;

    public string? Bio { get; set; }

    public string? Location { get; set; }

    public string? FarmingInterests { get; set; }

    public DateTime JoinedAt { get; set; }

    public bool IsCurrentUser { get; set; }

    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();

    public int PublicPostCount { get; set; }

    public int CommentCount { get; set; }

    public int LikesReceived { get; set; }

    public int ContributionScore { get; set; }

    public string ContributionLevel { get; set; } = string.Empty;

    public string? NextLevelName { get; set; }

    public int? PointsToNextLevel { get; set; }

    public IReadOnlyList<ContributionBadgeViewModel> Badges { get; set; } = Array.Empty<ContributionBadgeViewModel>();

    public IReadOnlyList<CommunityActivityViewModel> RecentActivity { get; set; } = Array.Empty<CommunityActivityViewModel>();
}

public sealed class ContributionBadgeViewModel
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Symbol { get; set; } = string.Empty;

    public bool IsEarned { get; set; }
}

public sealed class CommunityActivityViewModel
{
    public string Type { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Preview { get; set; } = string.Empty;

    public int ForumPostId { get; set; }

    public int? ForumCommentId { get; set; }

    public DateTime CreatedAt { get; set; }

    public int LikeCount { get; set; }

    public int CommentCount { get; set; }
}
