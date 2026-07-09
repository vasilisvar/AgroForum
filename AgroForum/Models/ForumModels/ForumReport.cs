using System.ComponentModel.DataAnnotations;

namespace AgroForum.Models.Forum
{
    public class ForumReport
    {
        public int Id { get; set; }

        public int? ForumPostId { get; set; }

        public ForumPost? ForumPost { get; set; }

        public int? ForumCommentId { get; set; }

        public ForumComment? ForumComment { get; set; }

        [Required]
        public string ReporterId { get; set; } = string.Empty;

        public ApplicationUser Reporter { get; set; } = null!;

        [Required]
        [MaxLength(80)]
        public string Reason { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Details { get; set; }

        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = ForumReportStatuses.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ReviewedAt { get; set; }

        public string? ReviewedById { get; set; }

        public ApplicationUser? ReviewedBy { get; set; }

        [MaxLength(1000)]
        public string? ModeratorNotes { get; set; }
    }
}
