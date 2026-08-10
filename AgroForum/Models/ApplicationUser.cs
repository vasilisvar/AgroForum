using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace AgroForum.Models;

public class ApplicationUser : IdentityUser
{
    [MaxLength(50)]
    public string? DisplayName { get; set; }

    [MaxLength(50)]
    public string? FirstName { get; set; }

    [MaxLength(50)]
    public string? LastName { get; set; }

    [MaxLength(500)]
    public string? Bio { get; set; }

    [MaxLength(100)]
    public string? Location { get; set; }

    [MaxLength(250)]
    public string? FarmingInterests { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
