using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScoreCast.Ws.Domain.V1.Entities.Football;
using ScoreCast.Ws.Infrastructure.V1.Shared;

namespace ScoreCast.Ws.Infrastructure.V1.Football.EntityConfigurations;

internal sealed class UserSeasonEntityConfiguration : BaseEntityConfiguration<UserSeason>
{
    public override void Configure(EntityTypeBuilder<UserSeason> builder)
    {
        base.Configure(builder);
        builder.ToTable("user_season");
        builder.HasKey(u => u.Id);
        var order = 1;

        builder.Property(u => u.UserId).HasColumnName("user_id").HasColumnOrder(order++).IsRequired();
        builder.Property(u => u.SeasonId).HasColumnName("season_id").HasColumnOrder(order++).IsRequired();

        builder.HasOne(u => u.User).WithMany().HasForeignKey(u => u.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(u => u.Season).WithMany().HasForeignKey(u => u.SeasonId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(u => new { u.UserId, u.SeasonId }).IsUnique();
    }
}
