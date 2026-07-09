using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AgroForum.ViewModels.Forum
{
    public class ReportContentViewModel
    {
        public int? ForumPostId { get; set; }

        public int? ForumCommentId { get; set; }

        public int ReturnForumPostId { get; set; }

        public string TargetType { get; set; } = string.Empty;

        public string TargetTitle { get; set; } = string.Empty;

        public string TargetPreview { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Reason")]
        public string Reason { get; set; } = string.Empty;

        [StringLength(1000)]
        [Display(Name = "Details")]
        public string? Details { get; set; }

        public IEnumerable<SelectListItem> ReasonOptions => new List<SelectListItem>
        {
            new() { Text = "Spam", Value = "Spam" },
            new() { Text = "Offensive content", Value = "Offensive content" },
            new() { Text = "Harassment", Value = "Harassment" },
            new() { Text = "Misinformation", Value = "Misinformation" },
            new() { Text = "Other", Value = "Other" }
        };
    }
}
