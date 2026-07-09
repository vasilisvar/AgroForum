using System.ComponentModel.DataAnnotations;

namespace AgroForum.Models.Forum
{
    public class ForumTag
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(70)]
        public string Slug { get; set; } = string.Empty;

        public ICollection<ForumPostTag> PostTags { get; set; } = new List<ForumPostTag>();
    }
}
