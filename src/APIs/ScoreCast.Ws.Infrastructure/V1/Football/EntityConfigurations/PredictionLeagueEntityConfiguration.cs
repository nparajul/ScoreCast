using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScoreCast.Ws.Domain.V1.Entities.Football;
using ScoreCast.Ws.Infrastructure.V1.Shared;

namespace ScoreCast.Ws.Infrastructure.V1.Football.EntityConfigurations;

internal sealed class PredictionLeagueEntityConfiguration : BaseEntityConfiguration<PredictionLeague>
{
    public override void Configure(EntityTypeBuilder<PredictionLeague> builder)
    {
        base.Configure(builder);
        builder.ToTable("prediction_league");
        builder.HasKey(p => p.Id);
        var order = 1;

        builder.Property(p => p.Name).HasColumnName("name").HasColumnOrder(order++).IsRequired().HasMaxLength(100);
        builder.Property(p => p.InviteCode).HasColumnName("invite_code").HasColumnOrder(order++).IsRequired().HasMaxLength(20);
        builder.Property(p => p.SeasonId).HasColumnName("season_id").HasColumnOrder(order++).IsRequired();
        builder.Property(p => p.CreatedByUserId).HasColumnName("created_by_user_id").HasColumnOrder(order++).IsRequired();

        builder.HasOne(p => p.Season).WithMany().HasForeignKey(p => p.SeasonId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.CreatedByUser).WithMany().HasForeignKey(p => p.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(p => p.InviteCode).IsUnique();
    }
}
