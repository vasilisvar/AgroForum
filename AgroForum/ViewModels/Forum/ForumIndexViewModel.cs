namespace AgroForum.ViewModels.Forum
{
    public class ForumIndexViewModel
    {
        public string? Search { get; set; }

        public string? Tag { get; set; }

        public IReadOnlyList<ForumPostSummaryViewModel> Posts { get; set; } = new List<ForumPostSummaryViewModel>();

        public IReadOnlyList<string> AvailableTags { get; set; } = new List<string>();
    }
}
