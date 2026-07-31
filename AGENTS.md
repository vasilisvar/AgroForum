# AgroForum Project Context

## Canonical location

- Repository root: `C:\Users\drtec\source\repos\AgroForum`
- Solution: `C:\Users\drtec\source\repos\AgroForum\AgroForum.sln`
- Web project: `C:\Users\drtec\source\repos\AgroForum\AgroForum`
- This is the only canonical working directory. Do not create Day copies, duplicate repositories, or project workspaces under `Documents\Codex`.
- The repository is outside the default Codex workspace in some sessions. Request narrowly scoped write/escalated access to the canonical repository when required; never work around missing access by creating another project copy.

## Git workflow

- `main` is the stable integration branch. Do not commit, push, rewrite, or merge `main` unless the user explicitly requests it.
- Each scheduled Day is a feature branch inside this same repository and working directory, not a separate folder.
- Inspect `git status -sb`, the current branch, and recent history before making changes.
- Commit the completed Day work to its feature branch and publish that branch to `origin` for review. The user performs the final merge into `main` manually unless they explicitly ask Codex to do it.
- Day 5 Admin/Moderator work used `feature/v2-admin-moderator-boards` and was merged through pull request #1.
- Day 6 design work uses `feature/day6-green-design`. Its published design commit is `b9c92f1` (`feat: improve overall look and redesign authentication forms`). Re-check the live branch state rather than assuming this hash remains the tip.

## Technology and data

- ASP.NET Core MVC and Razor Pages targeting .NET 8.
- ASP.NET Core Identity with `ApplicationUser` and Identity roles.
- Entity Framework Core with SQL Server LocalDB.
- Connection name: `DefaultConnection`.
- No default administrator credentials exist. Never hard-code or expose passwords, hashes, connection secrets, or user secrets.
- The supported first-admin workflow is: register a normal account, temporarily set `BootstrapAdmin:Email`, restart so the role is granted, then remove the temporary setting.
- Roles are exactly `Admin`, `Moderator`, `Expert`, and `Farmer`.

## Important endpoints

- Home: `/`
- Forum: `/Forum`
- Login: `/Identity/Account/Login`
- Register: `/Identity/Account/Register`
- Admin: `/Admin` (`Admin` role only)
- Moderation: `/Moderation` (`Admin` or `Moderator` role)
- Default HTTPS development URL: `https://localhost:7053`
- Default HTTP development URL: `http://localhost:5172`

## Completed milestones

- Day 5: role-protected Admin and Moderator boards, ticket/report workflow, moderator assignment, soft deletion/restoration, content controls, audit trail, role management, and secure first-admin bootstrapping.
- Day 6: lighter agricultural visual system; responsive homepage; live homepage discussions, tags, and totals; redesigned shared navigation/footer; restyled Forum/Admin/Moderation surfaces; custom Login and Register Razor Pages using the approved Split Farm Story layout; optional first and last name registration fields.

## Day 6 visual direction

- Friendly, bright agricultural community design rather than a dark enterprise theme.
- Primary leaf green: `#238636`.
- Supporting medium green: `#4F9D62`.
- Light botanical green: `#CFE8D2`.
- Soft sage: `#D8ECD9`.
- Pale green-white: `#F1F8F2`.
- Sun accent: `#E9B949`.
- Preserve white cards and accessible contrast. Do not use `#123524` as a large background.
- Authentication choice: Split Farm Story, with an illustrated agricultural story panel beside the focused form panel.

## Collaboration preferences

- Before an important visual or product-direction change, propose at least two coherent options and let the user choose or suggest another.
- Ask once for approval at meaningful structural, destructive, publishing, or security boundaries; do not ask for permission on every small implementation step.
- Preserve working functionality, database data, authentication, authorization, and role behavior during design changes.
- Keep progress updates concise and outcome-focused.

## Verification notes

- Run `dotnet build --no-restore` after relevant changes when packages are already restored.
- A running `AgroForum.exe` locks `bin\Debug\net8.0\AgroForum.exe` and causes MSB3021/MSB3027. Stop the running app with Visual Studio Shift+F5, terminal Ctrl+C, or the specific process ID before an in-place build. An isolated output build with `-p:UseAppHost=false` is a non-disruptive validation alternative.
- LocalDB and Windows Event Log access may be unavailable in a sandbox even when compilation succeeds. Distinguish environment restrictions from application failures.
- Keep the working tree clean at handoff and report the active branch, commit, build result, and whether anything was pushed or merged.
