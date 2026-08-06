namespace AgroForum.ViewModels.Forum
{
    public class SavedDiscussionsViewModel
    {
        public IReadOnlyList<ForumPostSummaryViewModel> Posts { get; set; } = new List<ForumPostSummaryViewModel>();
    }
}
