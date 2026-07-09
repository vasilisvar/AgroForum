namespace AgroForum.ViewModels.Forum
{
    public class ForumPostDetailsViewModel
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public string AuthorName { get; set; } = string.Empty;

        public bool IsAnonymous { get; set; }

        public bool IsLocked { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public IReadOnlyList<ForumTagViewModel> Tags { get; set; } = new List<ForumTagViewModel>();

        public IReadOnlyList<ForumCommentViewModel> Comments { get; set; } = new List<ForumCommentViewModel>();

        public CreateForumCommentViewModel NewComment { get; set; } = new();
    }
}
