using System.Text.RegularExpressions;
using AgroForum.Data;
using AgroForum.Helpers;
using AgroForum.Services.Community;
using AgroForum.ViewModels.Community;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AgroForum.Models;

namespace AgroForum.Controllers;

public sealed class CommunityController : Controller
{
    private const int RecentActivityLimit = 12;

    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ContributionBadgeService _badgeService;

    public CommunityController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ContributionBadgeService badgeService)
    {
        _context = context;
        _userManager = userManager;
        _badgeService = badgeService;
    }

    [AllowAnonymous]
    public async Task<IActionResult> Profile(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return NotFound();
        }

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id);

        if (user == null)
        {
            return NotFound();
        }

        var publicPosts = _context.ForumPosts
            .AsNoTracking()
            .Where(post => post.AuthorId == id && !post.IsDeleted && !post.IsAnonymous);

        var publicComments = _context.ForumComments
            .AsNoTracking()
            .Where(comment => comment.AuthorId == id && !comment.IsDeleted && !comment.ForumPost.IsDeleted);

        var postCount = await publicPosts.CountAsync();
        var commentCount = await publicComments.CountAsync();
        var likesReceived = await _context.ForumPostLikes
            .AsNoTracking()
            .CountAsync(like =>
                like.ForumPost.AuthorId == id &&
                !like.ForumPost.IsDeleted &&
                !like.ForumPost.IsAnonymous);

        var recentPosts = await publicPosts
            .OrderByDescending(post => post.CreatedAt)
            .Take(RecentActivityLimit)
            .Select(post => new
            {
                post.Id,
                post.Title,
                post.Content,
                post.CreatedAt,
                LikeCount = post.Likes.Count,
                CommentCount = post.Comments.Count(comment => !comment.IsDeleted)
            })
            .ToListAsync();

        var recentComments = await publicComments
            .OrderByDescending(comment => comment.CreatedAt)
            .Take(RecentActivityLimit)
            .Select(comment => new
            {
                comment.Id,
                comment.ForumPostId,
                PostTitle = comment.ForumPost.Title,
                comment.Content,
                comment.CreatedAt
            })
            .ToListAsync();

        var recentActivity = recentPosts
            .Select(post => new CommunityActivityViewModel
            {
                Type = "Discussion",
                Title = post.Title,
                Preview = BuildPreview(post.Content),
                ForumPostId = post.Id,
                CreatedAt = post.CreatedAt,
                LikeCount = post.LikeCount,
                CommentCount = post.CommentCount
            })
            .Concat(recentComments.Select(comment => new CommunityActivityViewModel
            {
                Type = "Comment",
                Title = comment.PostTitle,
                Preview = BuildPreview(comment.Content),
                ForumPostId = comment.ForumPostId,
                ForumCommentId = comment.Id,
                CreatedAt = comment.CreatedAt
            }))
            .OrderByDescending(activity => activity.CreatedAt)
            .Take(RecentActivityLimit)
            .ToList();

        var normalizedRoleNames = await (
            from userRole in _context.UserRoles.AsNoTracking()
            join role in _context.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            where userRole.UserId == id
            orderby role.Name
            select role.Name!)
            .ToListAsync();

        var contribution = _badgeService.Build(postCount, commentCount, likesReceived);
        var displayName = CommunityDisplayName.For(user);
        var model = new CommunityProfileViewModel
        {
            UserId = user.Id,
            DisplayName = displayName,
            Initial = displayName[..1].ToUpperInvariant(),
            Bio = user.Bio,
            Location = user.Location,
            FarmingInterests = user.FarmingInterests,
            JoinedAt = user.CreatedAt,
            IsCurrentUser = _userManager.GetUserId(User) == user.Id,
            Roles = normalizedRoleNames,
            PublicPostCount = postCount,
            CommentCount = commentCount,
            LikesReceived = likesReceived,
            ContributionScore = contribution.Score,
            ContributionLevel = contribution.LevelName,
            NextLevelName = contribution.NextLevelName,
            PointsToNextLevel = contribution.PointsToNextLevel,
            Badges = contribution.Badges,
            RecentActivity = recentActivity
        };

        return View(model);
    }

    private static string BuildPreview(string content)
    {
        var preview = Regex.Replace(content, "\\s+", " ").Trim();
        return preview.Length <= 170 ? preview : $"{preview[..170]}...";
    }
}
