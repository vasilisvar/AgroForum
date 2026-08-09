using AgroForum.ViewModels.Community;

namespace AgroForum.Services.Community;

public sealed class ContributionBadgeService
{
    public ContributionSummary Build(int postCount, int commentCount, int likesReceived)
    {
        var score = (postCount * 5) + (commentCount * 2) + likesReceived;
        var contributionCount = postCount + commentCount;

        var badges = new List<ContributionBadgeViewModel>
        {
            new()
            {
                Name = "First Sprout",
                Description = "Shared a first public discussion or comment.",
                Symbol = "🌱",
                IsEarned = contributionCount >= 1
            },
            new()
            {
                Name = "Field Voice",
                Description = "Published five public discussions.",
                Symbol = "🌾",
                IsEarned = postCount >= 5
            },
            new()
            {
                Name = "Helpful Hand",
                Description = "Received ten likes on public discussions.",
                Symbol = "🤝",
                IsEarned = likesReceived >= 10
            },
            new()
            {
                Name = "Community Cultivator",
                Description = "Reached 50 contribution points.",
                Symbol = "🏅",
                IsEarned = score >= 50
            }
        };

        var levels = new[]
        {
            new ContributionLevel("Seedling", 0),
            new ContributionLevel("Sprout", 10),
            new ContributionLevel("Cultivator", 30),
            new ContributionLevel("Harvest Guide", 75)
        };

        var currentLevel = levels.Last(level => score >= level.MinimumScore);
        var nextLevel = levels.FirstOrDefault(level => level.MinimumScore > score);

        return new ContributionSummary(
            score,
            currentLevel.Name,
            nextLevel?.Name,
            nextLevel == null ? null : nextLevel.MinimumScore - score,
            badges);
    }

    private sealed record ContributionLevel(string Name, int MinimumScore);
}

public sealed record ContributionSummary(
    int Score,
    string LevelName,
    string? NextLevelName,
    int? PointsToNextLevel,
    IReadOnlyList<ContributionBadgeViewModel> Badges);
