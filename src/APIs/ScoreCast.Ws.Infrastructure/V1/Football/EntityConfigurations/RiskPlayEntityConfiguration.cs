using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScoreCast.Ws.Domain.V1.Entities.Football;
using ScoreCast.Ws.Infrastructure.V1.Shared;

namespace ScoreCast.Ws.Infrastructure.V1.Football.EntityConfigurations;

internal sealed class RiskPlayEntityConfiguration : BaseEntityConfiguration<RiskPlay>
{
    public override void Configure(EntityTypeBuilder<RiskPlay> builder)
    {
        base.Configure(builder);
        builder.ToTable("risk_play");
        builder.HasKey(r => r.Id);
        var order = 1;

        builder.Property(r => r.SeasonId).HasColumnName("season_id").HasColumnOrder(order++).IsRequired();
        builder.Property(r => r.GameweekId).HasColumnName("gameweek_id").HasColumnOrder(order++).IsRequired();
        builder.Property(r => r.UserId).HasColumnName("user_id").HasColumnOrder(order++).IsRequired();
        builder.Property(r => r.MatchId).HasColumnName("match_id").HasColumnOrder(order++).IsRequired();
        builder.Property(r => r.RiskType).HasColumnName("risk_type").HasColumnOrder(order++).IsRequired().HasConversion<string>().HasMaxLength(30);
        builder.Property(r => r.Selection).HasColumnName("selection").HasColumnOrder(order++).HasMaxLength(200);
        builder.Property(r => r.BonusPoints).HasColumnName("bonus_points").HasColumnOrder(order++);
        builder.Property(r => r.IsResolved).HasColumnName("is_resolved").HasColumnOrder(order++);
        builder.Property(r => r.IsWon).HasColumnName("is_won").HasColumnOrder(order++);

        builder.HasOne(r => r.Season).WithMany().HasForeignKey(r => r.SeasonId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.Gameweek).WithMany().HasForeignKey(r => r.GameweekId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.User).WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.Match).WithMany().HasForeignKey(r => r.MatchId).OnDelete(DeleteBehavior.Restrict);

        // One risk type per match per user
        builder.HasIndex(r => new { r.UserId, r.MatchId, r.RiskType }).IsUnique();
        // Enforce limits per gameweek in application layer
    }
}
