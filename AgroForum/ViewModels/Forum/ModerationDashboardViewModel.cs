namespace AgroForum.ViewModels.Forum
{
    public class ModerationDashboardViewModel
    {
        public IReadOnlyList<ModerationReportViewModel> PendingReports { get; set; } = new List<ModerationReportViewModel>();

        public IReadOnlyList<ModerationReportViewModel> ResolvedReports { get; set; } = new List<ModerationReportViewModel>();
    }
}
