namespace AgroForum.ViewModels.Forum
{
    public class ForumPostSummaryViewModel
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Preview { get; set; } = string.Empty;

        public string AuthorName { get; set; } = string.Empty;

        public bool IsAnonymous { get; set; }

        public bool IsAuthorModerator { get; set; }

        public bool IsLocked { get; set; }

        public bool IsPinned { get; set; }

        public DateTime CreatedAt { get; set; }

        public int CommentCount { get; set; }

        public int FavoriteCount { get; set; }

        public IReadOnlyList<string> Tags { get; set; } = new List<string>();
    }
}
