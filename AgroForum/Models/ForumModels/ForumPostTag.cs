namespace AgroForum.Models.Forum
{
    public class ForumPostTag
    {
        public int ForumPostId { get; set; }


        /*many to many relationship between ForumPost and ForumTag*/
        public ForumPost ForumPost { get; set; } = null!;

        public int ForumTagId { get; set; }

        public ForumTag ForumTag { get; set; } = null!;
    }
}
