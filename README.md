# AgroForum

AgroForum is an ASP.NET Core MVC community forum for agricultural discussions.

## Day 6 design refresh

The Day 6 working copy introduces a brighter agricultural visual system built around leaf green, light botanical green, soft sage, and white surfaces. The responsive homepage now presents real recent discussions, popular tags, and community totals from the application database. The shared navigation, footer, forum, moderation, and administration surfaces use the same design tokens while preserving all existing role checks and endpoints.

Login and registration now use custom, responsive Razor Pages with the approved Split Farm Story layout. They preserve ASP.NET Identity validation and sign-in behavior while adding optional first and last names to registration.

## v2 Admin and Moderator boards

Version 2 adds two role-protected workspaces:

- `/Moderation` provides a claimable report queue, documented decisions, reversible content controls, filters, personal activity, and recent ticket outcomes.
- `/Admin` provides moderator-role management, global ticket oversight, completed-ticket reopening, feed pinning, content restoration, moderator metrics, and a complete moderation audit.

Reported content is handled as a ticket with `Open`, `InReview`, `Resolved`, and `Dismissed` states. Posts and comments are soft-deleted so an Admin can restore them. Moderation actions are stored in an append-only application audit.

## First Admin

There are no default credentials. Register the intended account normally, configure its email through user secrets or an environment variable, and restart the application:

```powershell
dotnet user-secrets set "BootstrapAdmin:Email" "admin@example.com" --project AgroForum/AgroForum.csproj
```

The configured account receives the `Admin` role during startup. Keep `BootstrapAdmin:Email` empty in committed configuration and store the real value only in secrets or deployment configuration.

## Database

Apply the EF Core migrations before starting v2:

```powershell
dotnet ef database update --project AgroForum/AgroForum.csproj --startup-project AgroForum/AgroForum.csproj
```

The v2 migration preserves existing reports by mapping `Pending` to `Open`, `Accepted` to `Resolved`, and `Rejected` to `Dismissed`.
