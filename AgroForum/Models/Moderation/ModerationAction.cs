using System.ComponentModel.DataAnnotations;

namespace AgroForum.Models.Moderation
{
    public class ModerationAction
    {
        public int Id { get; set; }

        [Required]
        public string ActorId { get; set; } = string.Empty;

        public ApplicationUser Actor { get; set; } = null!;

        [Required]
        [MaxLength(80)]
        public string ActionType { get; set; } = string.Empty;

        [Required]
        [MaxLength(40)]
        public string TargetType { get; set; } = string.Empty;

        [MaxLength(450)]
        public string? TargetId { get; set; }

        [MaxLength(200)]
        public string? TargetDisplay { get; set; }

        [MaxLength(1000)]
        public string? Details { get; set; }

        public int? ForumReportId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
