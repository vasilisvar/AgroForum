using System.ComponentModel.DataAnnotations;

namespace AgroForum.ViewModels.Forum
{
    public class CreateForumPostViewModel
    {
        [Required]
        [StringLength(150, MinimumLength = 5)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(5000, MinimumLength = 10)]
        [Display(Name = "Post content")]
        public string Content { get; set; } = string.Empty;

        [StringLength(300)]
        [Display(Name = "Tags")]
        public string? Tags { get; set; }

        [Display(Name = "Post anonymously")]
        public bool IsAnonymous { get; set; }
    }
}
