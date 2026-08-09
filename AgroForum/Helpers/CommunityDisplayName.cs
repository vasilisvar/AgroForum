using AgroForum.Models;

namespace AgroForum.Helpers;

public static class CommunityDisplayName
{
    public static string For(ApplicationUser? user)
    {
        if (user == null)
        {
            return "Community member";
        }

        if (!string.IsNullOrWhiteSpace(user.DisplayName))
        {
            return user.DisplayName.Trim();
        }

        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName)
            ? BuildFallback(user.Id)
            : fullName;
    }

    public static string CreateInitial(string? firstName, string? lastName, string userId)
    {
        var fullName = $"{firstName} {lastName}".Trim();
        if (!string.IsNullOrWhiteSpace(fullName))
        {
            return fullName.Length <= 50 ? fullName : fullName[..50].TrimEnd();
        }

        return BuildFallback(userId);
    }

    private static string BuildFallback(string userId)
    {
        var identifier = new string(userId.Where(char.IsLetterOrDigit).Take(6).ToArray()).ToUpperInvariant();
        return string.IsNullOrWhiteSpace(identifier) ? "Community member" : $"Farmer-{identifier}";
    }
}
