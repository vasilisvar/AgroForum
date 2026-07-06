using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace AgroForum.Models;

public class ApplicationUser : IdentityUser
{
    [MaxLength(50)]
    public string? FirstName { get; set; }

    [MaxLength(50)]
    public string? LastName { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
