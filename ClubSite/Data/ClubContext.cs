using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ClubSite.Models;
using System;

namespace ClubSite.Data;

public class ClubContext : IdentityDbContext<User, Role, string>
{
    public ClubContext(DbContextOptions<ClubContext> options)
        : base(options)
    {
    }

    public DbSet<ClubMembership> ClubMemberships { get; set; } = null!;

    public DbSet<MemberProfile> MemberProfiles { get; set; } = null!;

    public DbSet<SubscriptionHistory> SubscriptionHistories { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // User
        builder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Id).HasMaxLength(128);
            entity.Property(u => u.MembershipNumber).HasMaxLength(50);
            entity.HasOne(u => u.Profile)
                .WithOne(p => p.User)
                .HasForeignKey<MemberProfile>(p => p.UserId);
            entity.HasIndex(u => u.MembershipNumber).HasDatabaseName("IX_User_MembershipNumber");
        });

        // Role
        builder.Entity<Role>(entity =>
        {
            entity.Property(r => r.Id).HasMaxLength(128);
            entity.Property(r => r.DisplayName).HasMaxLength(100);
            entity.Property(r => r.Description).HasMaxLength(500);
            entity.Property(r => r.Rank).HasDefaultValue(0);
        });

        // ClubMembership
        builder.Entity<ClubMembership>(entity =>
        {
            entity.HasKey(cm => cm.Id);
            entity.Property(cm => cm.JoinDate).HasDefaultValueSql("GETDATE()");
            entity.Property(cm => cm.StatusChangeDate).HasDefaultValueSql("GETDATE()");
            entity.Property(cm => cm.Status).HasMaxLength(50);
            entity.HasOne(cm => cm.User)
                .WithMany(u => u.Memberships)
                .HasForeignKey(cm => cm.UserId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(cm => cm.Role)
                .WithMany(r => r.Memberships)
                .HasForeignKey(cm => cm.RoleId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // MemberProfile
        builder.Entity<MemberProfile>(entity =>
        {
            entity.HasKey(p => p.UserId);
            entity.Property(p => p.UserId).HasMaxLength(128);
            entity.Property(p => p.HomeAddress).HasMaxLength(200);
            entity.Property(p => p.Phone).HasMaxLength(200);
            entity.Property(p => p.City).HasMaxLength(100);
            entity.Property(p => p.State).HasMaxLength(50);
            entity.Property(p => p.ZipCode).HasMaxLength(10);
            entity.Property(p => p.EmergencyContactInfo).HasMaxLength(500);
            entity.Property(p => p.Preferences).HasMaxLength(1000);
            entity.Property(p => p.SpecialNeeds).HasMaxLength(500);
        });

        // SubscriptionHistory
        builder.Entity<SubscriptionHistory>(entity =>
        {
            entity.HasKey(h => h.Id);
            entity.Property(h => h.EffectiveDate).HasDefaultValueSql("GETDATE()");
            entity.Property(h => h.ChangeType).HasMaxLength(50);
            entity.Property(h => h.Details).HasMaxLength(1000);
            entity.HasOne(h => h.User)
                .WithMany(u => u.SubscriptionHistory)
                .HasForeignKey(h => h.UserId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(h => h.Role)
                .WithMany()
                .HasForeignKey(h => h.RoleId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(h => h.ClubMembership)
                .WithMany(cm => cm.SubscriptionHistory)
                .HasForeignKey(h => h.ClubMembershipId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Seed default roles
        var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        builder.Entity<Role>().HasData(
            new Role
            {
                Id = "admin", Name = "Admin", NormalizedName = "ADMIN",
                DisplayName = "Administrator", Description = "Full system administrator",
                Rank = 100, IsActive = true, CreatedOn = seedDate, LastModified = null
            },
            new Role
            {
                Id = "member", Name = "Member", NormalizedName = "MEMBER",
                DisplayName = "Member", Description = "Regular club member",
                Rank = 10, IsActive = true, CreatedOn = seedDate, LastModified = null
            },
            new Role
            {
                Id = "guest", Name = "Guest", NormalizedName = "GUEST",
                DisplayName = "Guest", Description = "Guest access",
                Rank = 1, IsActive = true, CreatedOn = seedDate, LastModified = null
            }
        );
    }
}