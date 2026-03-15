using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScoreCast.Ws.Domain.V1.Entities.Football;
using ScoreCast.Ws.Infrastructure.V1.Shared;

namespace ScoreCast.Ws.Infrastructure.V1.Football.EntityConfigurations;

internal sealed class MatchLineupEntityConfiguration : BaseEntityConfiguration<MatchLineup>
{
    public override void Configure(EntityTypeBuilder<MatchLineup> builder)
    {
        base.Configure(builder);
        builder.ToTable("match_lineup");
        builder.HasKey(e => e.Id);
        var order = 1;

        builder.Property(e => e.MatchId).HasColumnName("match_id").HasColumnOrder(order++).IsRequired();
        builder.Property(e => e.PlayerId).HasColumnName("player_id").HasColumnOrder(order++).IsRequired();
        builder.Property(e => e.IsStarter).HasColumnName("is_starter").HasColumnOrder(order++).IsRequired();

        builder.HasOne(e => e.Match).WithMany().HasForeignKey(e => e.MatchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Player).WithMany().HasForeignKey(e => e.PlayerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => new { e.MatchId, e.PlayerId }).IsUnique();
    }
}
