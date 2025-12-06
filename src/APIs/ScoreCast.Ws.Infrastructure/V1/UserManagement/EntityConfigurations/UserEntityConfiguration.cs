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

        var order = 1;

        builder.Property(u => u.KeycloakUserId)
            .HasColumnName("keycloak_user_id")
            .HasColumnOrder(order++)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(u => u.UserId)
            .HasColumnName("user_id")
            .HasColumnOrder(order++)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.Email)
            .HasColumnName("email")
            .HasColumnOrder(order++)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(u => u.DisplayName)
            .HasColumnName("display_name")
            .HasColumnOrder(order++)
            .HasMaxLength(100);

        builder.Property(u => u.AvatarUrl)
            .HasColumnName("avatar_url")
            .HasColumnOrder(order++)
            .HasMaxLength(500);

        builder.Property(u => u.FavoriteTeam)
            .HasColumnName("favorite_team")
            .HasColumnOrder(order++)
            .HasMaxLength(100);

        builder.Property(u => u.TotalPoints)
            .HasColumnName("total_points")
            .HasColumnOrder(order++)
            .HasDefaultValue(0);

        builder.Property(u => u.CurrentStreak)
            .HasColumnName("current_streak")
            .HasColumnOrder(order++)
            .HasDefaultValue(0);

        builder.Property(u => u.LongestStreak)
            .HasColumnName("longest_streak")
            .HasColumnOrder(order++)
            .HasDefaultValue(0);

        builder.Property(u => u.IsActive)
            .HasColumnName("is_active")
            .HasColumnOrder(order++)
            .HasDefaultValue(true);

        builder.Property(u => u.LastLoginDate)
            .HasColumnName("last_login_date")
            .HasColumnOrder(order++);

        builder.HasIndex(u => u.KeycloakUserId).IsUnique();
        builder.HasIndex(u => u.UserId).IsUnique();
        builder.HasIndex(u => u.Email).IsUnique();
    }
}
