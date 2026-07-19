using AgroForum.ViewModels.Forum;

namespace AgroForum.ViewModels.Admin
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }

        public int ModeratorCount { get; set; }

        public int ActiveTicketCount { get; set; }

        public int DeletedContentCount { get; set; }

        public string Search { get; set; } = string.Empty;

        public IReadOnlyList<AdminUserViewModel> Users { get; set; } = new List<AdminUserViewModel>();

        public IReadOnlyList<ModeratorSummaryViewModel> Moderators { get; set; } = new List<ModeratorSummaryViewModel>();

        public IReadOnlyList<AdminTicketViewModel> ActiveTickets { get; set; } = new List<AdminTicketViewModel>();

        public IReadOnlyList<AdminTicketViewModel> RecentCompletedTickets { get; set; } = new List<AdminTicketViewModel>();

        public IReadOnlyList<AdminContentViewModel> RecentPosts { get; set; } = new List<AdminContentViewModel>();

        public IReadOnlyList<DeletedContentViewModel> DeletedContent { get; set; } = new List<DeletedContentViewModel>();

        public IReadOnlyList<ModerationActivityViewModel> Activity { get; set; } = new List<ModerationActivityViewModel>();
    }

    public class AdminUserViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsModerator { get; set; }
        public bool IsAdmin { get; set; }
    }

    public class ModeratorSummaryViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int ActiveTicketCount { get; set; }
        public int ResolvedTicketCount { get; set; }
        public int ActionCount { get; set; }
        public DateTime? LastActiveAt { get; set; }
    }

    public class AdminTicketViewModel
    {
        public int Id { get; set; }
        public string TargetType { get; set; } = string.Empty;
        public string TargetTitle { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string AssignedModeratorName { get; set; } = "Unassigned";
        public string? AssignedToId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AdminContentViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public bool IsPinned { get; set; }
        public bool IsLocked { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class DeletedContentViewModel
    {
        public string TargetType { get; set; } = string.Empty;
        public int TargetId { get; set; }
        public int ForumPostId { get; set; }
        public string TargetDisplay { get; set; } = string.Empty;
        public string? DeletionReason { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
