using System.ComponentModel.DataAnnotations;

namespace AgroForum.Models.Forum
{
    public class ForumPost
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(5000)]
        public string Content { get; set; } = string.Empty;

        public bool IsAnonymous { get; set; } = false;

        public bool IsDeleted { get; set; } = false;

        public bool IsLocked { get; set; } = false;

        public bool IsPinned { get; set; } = false;

        public DateTime? DeletedAt { get; set; }

        public string? DeletedByUserId { get; set; }

        public ApplicationUser? DeletedByUser { get; set; }

        [MaxLength(500)]
        public string? DeletionReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [Required]
        public string AuthorId { get; set; } = string.Empty;

        public ApplicationUser Author { get; set; } = null!;

        public ICollection<ForumComment> Comments { get; set; } = new List<ForumComment>();

        public ICollection<ForumPostTag> PostTags { get; set; } = new List<ForumPostTag>();

        public ICollection<ForumPostLike> Likes { get; set; } = new List<ForumPostLike>();

        public ICollection<ForumPostFavorite> Favorites { get; set; } = new List<ForumPostFavorite>();

        public ICollection<ForumReport> Reports { get; set; } = new List<ForumReport>();
    }
}
