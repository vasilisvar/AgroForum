namespace AgroForum.Models.Moderation
{
    public static class ModerationActionTypes
    {
        public const string TicketClaimed = "Ticket claimed";
        public const string TicketReleased = "Ticket released";
        public const string TicketReassigned = "Ticket reassigned";
        public const string TicketReopened = "Ticket reopened";
        public const string TicketDismissed = "Ticket dismissed";
        public const string PostRemoved = "Post removed";
        public const string CommentRemoved = "Comment removed";
        public const string PostRestored = "Post restored";
        public const string CommentRestored = "Comment restored";
        public const string PostLocked = "Post locked";
        public const string PostUnlocked = "Post unlocked";
        public const string PostPinned = "Post pinned";
        public const string PostUnpinned = "Post unpinned";
        public const string TagAdded = "Tag added";
        public const string TagRemoved = "Tag removed";
        public const string ModeratorGranted = "Moderator role granted";
        public const string ModeratorRevoked = "Moderator role revoked";
    }
}
