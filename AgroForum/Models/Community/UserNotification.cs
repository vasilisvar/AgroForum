using System.ComponentModel.DataAnnotations;
using AgroForum.Models.Forum;

namespace AgroForum.Models.Community;

public class UserNotification
{
    public long Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    public string? ActorId { get; set; }

    public ApplicationUser? Actor { get; set; }

    [Required]
    [MaxLength(40)]
    public string Type { get; set; } = string.Empty;

    public int? ForumPostId { get; set; }

    public ForumPost? ForumPost { get; set; }

    public int? ForumCommentId { get; set; }

    public ForumComment? ForumComment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReadAt { get; set; }
}
