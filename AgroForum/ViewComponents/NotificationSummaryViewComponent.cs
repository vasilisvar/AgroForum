using AgroForum.Data;
using AgroForum.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgroForum.ViewComponents;

public sealed class NotificationSummaryViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public NotificationSummaryViewComponent(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Content(string.Empty);
        }

        var userId = _userManager.GetUserId(UserClaimsPrincipal);
        if (userId == null)
        {
            return Content(string.Empty);
        }

        var unreadCount = await _context.UserNotifications
            .AsNoTracking()
            .CountAsync(notification => notification.UserId == userId && notification.ReadAt == null);

        return View(unreadCount);
    }
}
