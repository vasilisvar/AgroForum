namespace AgroForum.ViewModels.Forum
{
    public class ModerationDashboardViewModel
    {
        public int OpenTicketCount { get; set; }

        public int MyTicketCount { get; set; }

        public int ResolvedTodayCount { get; set; }

        public string StatusFilter { get; set; } = "Active";

        public string TargetTypeFilter { get; set; } = string.Empty;

        public string AssignmentFilter { get; set; } = "All";

        public IReadOnlyList<ModerationReportViewModel> Tickets { get; set; } = new List<ModerationReportViewModel>();

        public IReadOnlyList<ModerationReportViewModel> RecentDecisions { get; set; } = new List<ModerationReportViewModel>();

        public IReadOnlyList<ModerationActivityViewModel> RecentActivity { get; set; } = new List<ModerationActivityViewModel>();
    }
}
