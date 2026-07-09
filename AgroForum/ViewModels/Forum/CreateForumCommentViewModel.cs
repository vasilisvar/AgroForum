using System.ComponentModel.DataAnnotations;

namespace AgroForum.ViewModels.Forum
{
    public class CreateForumCommentViewModel
    {
        public int ForumPostId { get; set; }

        [Required]
        [StringLength(3000, MinimumLength = 2)]
        [Display(Name = "Comment")]
        public string Content { get; set; } = string.Empty;
    }
}
