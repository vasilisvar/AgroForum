namespace AgroForum.ViewModels.Forum
{
    public class ModerationReportViewModel
    {
        public int Id { get; set; }

        public string TargetType { get; set; } = string.Empty;

        public int? ForumPostId { get; set; }

        public int? ForumCommentId { get; set; }

        public string TargetTitle { get; set; } = string.Empty;

        public string TargetContent { get; set; } = string.Empty;

        public string TargetAuthorName { get; set; } = string.Empty;

        public string ReporterName { get; set; } = string.Empty;

        public string Reason { get; set; } = string.Empty;

        public string? Details { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime? AssignedAt { get; set; }

        public DateTime? ReviewedAt { get; set; }

        public string AssignedModeratorName { get; set; } = "Unassigned";

        public string? ReviewerName { get; set; }

        public string? ModeratorNotes { get; set; }

        public bool IsAssignedToCurrentUser { get; set; }

        public bool CanHandle { get; set; }

        public bool CanClaim { get; set; }

        public bool IsPostLocked { get; set; }

        public bool IsTargetDeleted { get; set; }
    }
}
