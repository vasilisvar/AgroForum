namespace AgroForum.Models.Forum
{
    public class ForumPostLike
    {
        public int ForumPostId { get; set; }

        public ForumPost ForumPost { get; set; } = null!;

        public string UserId { get; set; } = string.Empty;

        public ApplicationUser User { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}