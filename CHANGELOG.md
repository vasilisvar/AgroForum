# Changelog

## Day 6 - In progress

### Agricultural visual system and homepage

- Added the approved light botanical-green, sage, leaf-green, and white design system.
- Rebuilt the homepage as a responsive, production-ready Razor view based on the selected visual concept.
- Added live homepage data for popular tags, recent discussions, members, discussions, and replies.
- Reworked the shared role-aware navigation, account actions, brand mark, and footer.
- Restyled forum, administration, moderation, forms, cards, tables, statuses, and responsive layouts without changing authorization behavior.
- Added custom Login and Register Razor Pages using the approved Split Farm Story design.
- Preserved Identity password hashing, validation, safe local return URLs, two-factor and lockout routing, and external-login provider hooks.
- Added optional first-name and last-name registration fields using the existing application-user columns.
- Preserved the Day 5 release as a separate working copy.

## 2.0.0 - 2026-07-19

### Admin / Moderator boards added

- Added separate role-protected Admin and Moderator workspaces.
- Added ticket claiming, assignment, reassignment, reopening, filtering, and concurrency protection.
- Added moderator-role granting and revocation with automatic public badges.
- Added append-only moderation activity auditing and Admin activity monitoring.
- Added post/comment soft deletion, Admin restoration, post locking, tag controls, and Admin-only feed pinning.
- Added a data-preserving EF Core migration and secure first-Admin bootstrapping.
