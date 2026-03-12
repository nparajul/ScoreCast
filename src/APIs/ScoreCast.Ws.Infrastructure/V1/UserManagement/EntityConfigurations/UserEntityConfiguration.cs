using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScoreCast.Ws.Domain.V1.Entities.UserManagement;
using ScoreCast.Ws.Infrastructure.V1.Shared;

namespace ScoreCast.Ws.Infrastructure.V1.UserManagement.EntityConfigurations;

internal sealed class UserEntityConfiguration : BaseEntityConfiguration<UserMaster>
{
    public override void Configure(EntityTypeBuilder<UserMaster> builder)
    {
        base.Configure(builder);

        builder.ToTable("user_master");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.KeycloakUserId)
            .HasColumnName("keycloak_user_id")
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(u => u.UserId)
            .HasColumnName("user_id")
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.Email)
            .HasColumnName("email")
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(u => u.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(100);

        builder.Property(u => u.AvatarUrl)
            .HasColumnName("avatar_url")
            .HasMaxLength(500);

        builder.Property(u => u.FavoriteTeam)
            .HasColumnName("favorite_team")
            .HasMaxLength(100);

        builder.Property(u => u.TotalPoints)
            .HasColumnName("total_points")
            .HasDefaultValue(0);

        builder.Property(u => u.CurrentStreak)
            .HasColumnName("current_streak")
            .HasDefaultValue(0);

        builder.Property(u => u.LongestStreak)
            .HasColumnName("longest_streak")
            .HasDefaultValue(0);

        builder.Property(u => u.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.HasIndex(u => u.KeycloakUserId).IsUnique();
        builder.HasIndex(u => u.UserId).IsUnique();
        builder.HasIndex(u => u.Email).IsUnique();
    }
}
