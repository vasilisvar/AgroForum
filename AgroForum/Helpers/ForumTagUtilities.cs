using System.Text.RegularExpressions;

namespace AgroForum.Helpers
{
    public static class ForumTagUtilities
    {
        public static IReadOnlyList<string> ParseTagNames(string? tagList)
        {
            if (string.IsNullOrWhiteSpace(tagList))
            {
                return new List<string>();
            }

            return tagList
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(NormalizeTagName)
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .GroupBy(CreateSlug)
                .Select(group => group.First())
                .Take(8)
                .ToList();
        }

        public static string CreateSlug(string name)
        {
            var normalized = NormalizeTagName(name).ToLowerInvariant();
            var characters = normalized.Select(character => char.IsLetterOrDigit(character) ? character : '-').ToArray();
            var slug = Regex.Replace(new string(characters), "-+", "-").Trim('-');

            if (string.IsNullOrWhiteSpace(slug))
            {
                return Guid.NewGuid().ToString("N");
            }

            return slug.Length <= 70 ? slug : slug[..70];
        }

        private static string NormalizeTagName(string value)
        {
            var normalized = Regex.Replace(value.Trim(), "\\s+", " ");

            if (normalized.Length <= 50)
            {
                return normalized;
            }

            return normalized[..50];
        }
    }
}
