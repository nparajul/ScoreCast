using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScoreCast.Ws.Domain.V1.Entities.Football;
using ScoreCast.Shared.Enums;
using ScoreCast.Ws.Infrastructure.V1.Shared;

namespace ScoreCast.Ws.Infrastructure.V1.Football.EntityConfigurations;

internal sealed class MatchEventEntityConfiguration : BaseEntityConfiguration<MatchEvent>
{
    public override void Configure(EntityTypeBuilder<MatchEvent> builder)
    {
        base.Configure(builder);
        builder.ToTable("match_event");
        builder.HasKey(e => e.Id);
        var order = 1;

        builder.Property(e => e.MatchId).HasColumnName("match_id").HasColumnOrder(order++).IsRequired();
        builder.Property(e => e.PlayerId).HasColumnName("player_id").HasColumnOrder(order++).IsRequired();
        builder.Property(e => e.EventType).HasColumnName("event_type").HasColumnOrder(order++).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(e => e.Value).HasColumnName("value").HasColumnOrder(order++).HasDefaultValue(1);
        builder.Property(e => e.Minute).HasColumnName("minute").HasColumnOrder(order++).HasMaxLength(10);

        builder.HasOne(e => e.Match).WithMany().HasForeignKey(e => e.MatchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Player).WithMany().HasForeignKey(e => e.PlayerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => new { e.MatchId, e.PlayerId, e.EventType, e.Minute }).IsUnique();
    }
}
