namespace AgroForum.ViewModels.Home;

public class HomeIndexViewModel
{
    public int MemberCount { get; set; }

    public int DiscussionCount { get; set; }

    public int ReplyCount { get; set; }

    public IReadOnlyList<HomeCategoryViewModel> Categories { get; set; } = new List<HomeCategoryViewModel>();

    public IReadOnlyList<HomeDiscussionViewModel> RecentDiscussions { get; set; } = new List<HomeDiscussionViewModel>();
}

public class HomeCategoryViewModel
{
    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public int DiscussionCount { get; set; }
}

public class HomeDiscussionViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Preview { get; set; } = string.Empty;

    public string AuthorName { get; set; } = string.Empty;

    public string? AuthorId { get; set; }

    public string? TagName { get; set; }

    public string? TagSlug { get; set; }

    public DateTime CreatedAt { get; set; }

    public int ReplyCount { get; set; }
}
