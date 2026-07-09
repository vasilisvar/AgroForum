using System.ComponentModel.DataAnnotations;

namespace AgroForum.Models.Forum
{
    public class ForumComment
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(3000)]
        public string Content { get; set; } = string.Empty;

        public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedAt { get; set; }

        public string? DeletedByUserId { get; set; }

        public ApplicationUser? DeletedByUser { get; set; }

        [MaxLength(500)]
        public string? DeletionReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public int ForumPostId { get; set; }

        public ForumPost ForumPost { get; set; } = null!;

        [Required]
        public string AuthorId { get; set; } = string.Empty;

        public ApplicationUser Author { get; set; } = null!;

        public ICollection<ForumReport> Reports { get; set; } = new List<ForumReport>();
    }
}