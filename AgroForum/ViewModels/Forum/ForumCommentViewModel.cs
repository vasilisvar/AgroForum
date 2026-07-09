namespace AgroForum.ViewModels.Forum
{
    public class ForumCommentViewModel
    {
        public int Id { get; set; }

        public string Content { get; set; } = string.Empty;

        public string AuthorName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; }

        public string? DeletionReason { get; set; }
    }
}
