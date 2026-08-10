namespace AgroForum.ViewModels.Forum
{
    public class ForumIndexViewModel
    {
        public string? Search { get; set; }

        public string? Tag { get; set; }

        public string Sort { get; set; } = "newest";

        public string Solution { get; set; } = "all";

        public int CurrentPage { get; set; } = 1;

        public int TotalPages { get; set; }

        public int TotalResults { get; set; }

        public bool HasPreviousPage => CurrentPage > 1;

        public bool HasNextPage => CurrentPage < TotalPages;

        public IReadOnlyList<ForumPostSummaryViewModel> Posts { get; set; } = new List<ForumPostSummaryViewModel>();

        public IReadOnlyList<ForumTagViewModel> AvailableTags { get; set; } = new List<ForumTagViewModel>();
    }
}
