# Changelog

## Version 6 - Community identity - Complete

- Added privacy-conscious public profiles with display names, biography, location, farming interests, roles, and join dates.
- Replaced public email-name fallbacks with generated farmer display names and added editable profile fields to Identity account settings.
- Added public activity history for visible discussions and comments while excluding anonymous discussion ownership.
- Added contribution scoring, four progressive levels, and earned badges for discussions, helpful activity, likes received, and sustained participation.
- Added persisted in-app notifications for new likes and comments on a member's discussions.
- Added an unread notification indicator, a private notification center, mark-all-read handling, and safe POST-based notification opening.
- Avoided notifications for private saves and prevented self-like or self-comment notifications.
- Removed stale like notifications when a like is withdrawn and preserved anonymous authors' ability to receive engagement notifications privately.
- Linked visible author names and avatars to profiles across the homepage, forum feed, saved discussions, discussion details, and comments.
- Added the default Farmer role for new accounts and data-preserving profile defaults for existing accounts.
- Added the `AddCommunityIdentity` migration with optional profile columns, a notification table, indexes, and non-destructive role backfill.
- Added responsive profile, badge, activity, notification, and profile-editing interfaces consistent with the AgroForum design system.

## Version 5 - Abuse prevention - Complete

- Added configurable reCAPTCHA v3 protection to registration, discussion creation, comments, and reports.
- Executes each challenge at submit time so short-lived tokens are sent to the server immediately.
- Verifies token success, expected action, score threshold, hostname, and timestamp on the server.
- Rejects missing, expired, replayed, mismatched, low-score, and malformed verification responses without exposing security details to visitors.
- Fails closed when enabled verification is unavailable while preserving a retry-friendly user message.
- Keeps the site key, secret key, score threshold, endpoint, and allowed hostnames in typed configuration.
- Validates enabled configuration at startup and keeps reCAPTCHA disabled by default until deployment-specific keys are supplied.
- Avoids transmitting the visitor's IP address because Google treats it as an optional verification parameter.

## Day 8 - Complete

- Preserved optional anonymous posting while keeping account ownership available internally for safety and moderation.
- Added one optional image per discussion with JPEG, PNG, and WebP support and a 5 MB limit.
- Added server-side extension, MIME-type, and binary-signature checks so renamed non-image files are rejected.
- Added randomized server filenames, a dedicated runtime upload directory, and cleanup when database persistence fails.
- Added a nullable, data-preserving `ImagePath` migration for existing discussions.
- Added responsive image presentation to the forum feed, saved discussions, and full discussion pages.
- Verified invalid-file rejection and valid anonymous image publication in an isolated LocalDB database at desktop and mobile sizes.

## Community engagement - Complete

- Activated the existing post-like and post-favorite relationships without requiring a new database migration.
- Added authenticated like/unlike and save/remove actions with anti-forgery protection and safe local redirects.
- Added per-user engagement states and updated community counts to forum cards and full discussion pages.
- Added a private Saved discussions page with save-date ordering, removal controls, and an agricultural empty state.
- Added signed-in Saved navigation and sign-in prompts that preserve the visitor's original destination.
- Added responsive, keyboard-visible active states consistent with the AgroForum visual system.

## Day 6 - Complete

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
