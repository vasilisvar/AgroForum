namespace AgroForum.ViewModels.Forum
{
    public class ForumPostDetailsViewModel
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public string? ImagePath { get; set; }

        public string? ResourceTitle { get; set; }

        public string? ResourceUrl { get; set; }

        public string AuthorName { get; set; } = string.Empty;

        public string? AuthorId { get; set; }

        public bool IsAnonymous { get; set; }

        public bool IsAuthorModerator { get; set; }

        public bool IsLocked { get; set; }

        public bool IsPinned { get; set; }

        public int? AcceptedCommentId { get; set; }

        public bool HasAcceptedSolution { get; set; }

        public bool CanManageSolution { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public int LikeCount { get; set; }

        public int FavoriteCount { get; set; }

        public bool IsLikedByCurrentUser { get; set; }

        public bool IsFavoritedByCurrentUser { get; set; }

        public IReadOnlyList<ForumTagViewModel> Tags { get; set; } = new List<ForumTagViewModel>();

        public IReadOnlyList<ForumCommentViewModel> Comments { get; set; } = new List<ForumCommentViewModel>();

        public IReadOnlyList<RelatedDiscussionViewModel> RelatedDiscussions { get; set; } = new List<RelatedDiscussionViewModel>();

        public CreateForumCommentViewModel NewComment { get; set; } = new();
    }

    public class RelatedDiscussionViewModel
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public int SharedTagCount { get; set; }

        public int CommentCount { get; set; }

        public bool HasAcceptedSolution { get; set; }
    }
}
