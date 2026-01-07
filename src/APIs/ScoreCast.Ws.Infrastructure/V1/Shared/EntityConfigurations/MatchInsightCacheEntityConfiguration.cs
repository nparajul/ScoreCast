using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScoreCast.Ws.Domain.V1.Entities.Football;

namespace ScoreCast.Ws.Infrastructure.V1.Shared.EntityConfigurations;

internal sealed class MatchInsightCacheEntityConfiguration : BaseEntityConfiguration<MatchInsightCache>
{
    public override void Configure(EntityTypeBuilder<MatchInsightCache> builder)
    {
        base.Configure(builder);
        builder.ToTable("match_insight_cache");
        builder.HasKey(m => m.Id);
        var order = 1;

        builder.Property(m => m.SeasonId).HasColumnName("season_id").HasColumnOrder(order++).IsRequired();
        builder.Property(m => m.GameweekNumber).HasColumnName("gameweek_number").HasColumnOrder(order++).IsRequired();
        builder.Property(m => m.ResponseJson).HasColumnName("response_json").HasColumnOrder(order).IsRequired();

        builder.HasIndex(m => new { m.SeasonId, m.GameweekNumber }).IsUnique();
        builder.HasOne(m => m.Season).WithMany().HasForeignKey(m => m.SeasonId);
    }
}
