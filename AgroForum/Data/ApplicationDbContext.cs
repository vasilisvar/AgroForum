using AgroForum.Models;
using AgroForum.Models.Forum;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AgroForum.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<ForumPost> ForumPosts { get; set; }
        public DbSet<ForumComment> ForumComments { get; set; }
        public DbSet<ForumTag> ForumTags { get; set; }
        public DbSet<ForumPostTag> ForumPostTags { get; set; }
        public DbSet<ForumPostLike> ForumPostLikes { get; set; }
        public DbSet<ForumPostFavorite> ForumPostFavorites { get; set; }
        public DbSet<ForumReport> ForumReports { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ForumPost>(entity =>
            {
                entity.Property(p => p.Title)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(p => p.Content)
                    .IsRequired()
                    .HasMaxLength(5000);

                entity.Property(p => p.DeletionReason)
                    .HasMaxLength(500);

                entity.HasOne(p => p.Author)
                    .WithMany()
                    .HasForeignKey(p => p.AuthorId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.DeletedByUser)
                    .WithMany()
                    .HasForeignKey(p => p.DeletedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<ForumComment>(entity =>
            {
                entity.Property(c => c.Content)
                    .IsRequired()
                    .HasMaxLength(3000);

                entity.Property(c => c.DeletionReason)
                    .HasMaxLength(500);

                entity.HasOne(c => c.ForumPost)
                    .WithMany(p => p.Comments)
                    .HasForeignKey(c => c.ForumPostId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(c => c.Author)
                    .WithMany()
                    .HasForeignKey(c => c.AuthorId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(c => c.DeletedByUser)
                    .WithMany()
                    .HasForeignKey(c => c.DeletedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<ForumTag>(entity =>
            {
                entity.Property(t => t.Name)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(t => t.Slug)
                    .IsRequired()
                    .HasMaxLength(70);

                entity.HasIndex(t => t.Name)
                    .IsUnique();

                entity.HasIndex(t => t.Slug)
                    .IsUnique();
            });

            builder.Entity<ForumPostTag>(entity =>
            {
                entity.HasKey(pt => new { pt.ForumPostId, pt.ForumTagId });

                entity.HasOne(pt => pt.ForumPost)
                    .WithMany(p => p.PostTags)
                    .HasForeignKey(pt => pt.ForumPostId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(pt => pt.ForumTag)
                    .WithMany(t => t.PostTags)
                    .HasForeignKey(pt => pt.ForumTagId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<ForumPostLike>(entity =>
            {
                entity.HasKey(l => new { l.ForumPostId, l.UserId });

                entity.HasOne(l => l.ForumPost)
                    .WithMany(p => p.Likes)
                    .HasForeignKey(l => l.ForumPostId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(l => l.User)
                    .WithMany()
                    .HasForeignKey(l => l.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<ForumPostFavorite>(entity =>
            {
                entity.HasKey(f => new { f.ForumPostId, f.UserId });

                entity.HasOne(f => f.ForumPost)
                    .WithMany(p => p.Favorites)
                    .HasForeignKey(f => f.ForumPostId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(f => f.User)
                    .WithMany()
                    .HasForeignKey(f => f.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<ForumReport>(entity =>
            {
                entity.Property(r => r.Reason)
                    .IsRequired()
                    .HasMaxLength(80);

                entity.Property(r => r.Details)
                    .HasMaxLength(1000);

                entity.Property(r => r.Status)
                    .IsRequired()
                    .HasMaxLength(30);

                entity.Property(r => r.ModeratorNotes)
                    .HasMaxLength(1000);

                entity.HasIndex(r => new { r.Status, r.CreatedAt });

                entity.HasOne(r => r.ForumPost)
                    .WithMany(p => p.Reports)
                    .HasForeignKey(r => r.ForumPostId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.ForumComment)
                    .WithMany(c => c.Reports)
                    .HasForeignKey(r => r.ForumCommentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.Reporter)
                    .WithMany()
                    .HasForeignKey(r => r.ReporterId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.ReviewedBy)
                    .WithMany()
                    .HasForeignKey(r => r.ReviewedById)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}