# AgroForum

AgroForum is an ASP.NET Core MVC community forum for agricultural discussions.

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
