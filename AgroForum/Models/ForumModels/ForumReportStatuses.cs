namespace AgroForum.Models.Forum
{
    public static class ForumReportStatuses
    {
        public const string Open = "Open";
        public const string InReview = "InReview";
        public const string Resolved = "Resolved";
        public const string Dismissed = "Dismissed";

        public static readonly string[] Active = { Open, InReview };
    }
}
