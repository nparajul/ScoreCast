using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScoreCast.Ws.Domain.V1.Entities.Football;
using ScoreCast.Shared.Enums;
using ScoreCast.Ws.Infrastructure.V1.Shared;

namespace ScoreCast.Ws.Infrastructure.V1.Football.EntityConfigurations;

internal sealed class MatchEntityConfiguration : BaseEntityConfiguration<Match>
{
    public override void Configure(EntityTypeBuilder<Match> builder)
    {
        base.Configure(builder);
        builder.ToTable("match");
        builder.HasKey(m => m.Id);
        var order = 1;

        builder.Property(m => m.GameweekId).HasColumnName("gameweek_id").HasColumnOrder(order++).IsRequired();
        builder.Property(m => m.HomeTeamId).HasColumnName("home_team_id").HasColumnOrder(order++).IsRequired();
        builder.Property(m => m.AwayTeamId).HasColumnName("away_team_id").HasColumnOrder(order++).IsRequired();
        builder.Property(m => m.ExternalId).HasColumnName("external_id").HasColumnOrder(order++).HasMaxLength(50);
        builder.Property(m => m.KickoffTime).HasColumnName("kickoff_time").HasColumnOrder(order++);
        builder.Property(m => m.HomeScore).HasColumnName("home_score").HasColumnOrder(order++);
        builder.Property(m => m.AwayScore).HasColumnName("away_score").HasColumnOrder(order++);
        builder.Property(m => m.Status).HasColumnName("status").HasColumnOrder(order++).HasConversion<string>().HasMaxLength(20).HasDefaultValue(MatchStatus.Scheduled);
        builder.Property(m => m.Venue).HasColumnName("venue").HasColumnOrder(order++).HasMaxLength(200);
        builder.Property(m => m.Referee).HasColumnName("referee").HasColumnOrder(order++).HasMaxLength(200);

        builder.HasOne(m => m.Gameweek).WithMany(g => g.Matches).HasForeignKey(m => m.GameweekId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(m => m.HomeTeam).WithMany().HasForeignKey(m => m.HomeTeamId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(m => m.AwayTeam).WithMany().HasForeignKey(m => m.AwayTeamId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(m => m.ExternalId).IsUnique().HasFilter("external_id IS NOT NULL");
        builder.HasIndex(m => new { m.GameweekId, m.HomeTeamId, m.AwayTeamId }).IsUnique();
    }
}
