namespace AgroForum.ViewModels.Forum
{
    public class ModerationActivityViewModel
    {
        public int Id { get; set; }

        public string ActorName { get; set; } = string.Empty;

        public string ActionType { get; set; } = string.Empty;

        public string TargetType { get; set; } = string.Empty;

        public string? TargetDisplay { get; set; }

        public string? Details { get; set; }

        public int? ForumReportId { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
