using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScoreCast.Shared.Enums;
using ScoreCast.Ws.Domain.V1.Entities.Football;
using ScoreCast.Ws.Infrastructure.V1.Shared;

namespace ScoreCast.Ws.Infrastructure.V1.Football.EntityConfigurations;

internal sealed class PredictionEntityConfiguration : BaseEntityConfiguration<Prediction>
{
    public override void Configure(EntityTypeBuilder<Prediction> builder)
    {
        base.Configure(builder);
        builder.ToTable("prediction");
        builder.HasKey(p => p.Id);
        var order = 1;

        builder.Property(p => p.SeasonId).HasColumnName("season_id").HasColumnOrder(order++).IsRequired();
        builder.Property(p => p.UserId).HasColumnName("user_id").HasColumnOrder(order++).IsRequired();
        builder.Property(p => p.MatchId).HasColumnName("match_id").HasColumnOrder(order++);
        builder.Property(p => p.PredictionType).HasColumnName("prediction_type").HasColumnOrder(order++).IsRequired().HasConversion<string>().HasMaxLength(30).HasDefaultValue(PredictionType.Score);
        builder.Property(p => p.PredictedHomeScore).HasColumnName("predicted_home_score").HasColumnOrder(order++);
        builder.Property(p => p.PredictedAwayScore).HasColumnName("predicted_away_score").HasColumnOrder(order++);
        builder.Property(p => p.PredictedTeamId).HasColumnName("predicted_team_id").HasColumnOrder(order++);
        builder.Property(p => p.PredictedPlayerId).HasColumnName("predicted_player_id").HasColumnOrder(order++);
        builder.Property(p => p.Outcome).HasColumnName("outcome").HasColumnOrder(order++).HasConversion<string>();

        builder.HasOne(p => p.Season).WithMany().HasForeignKey(p => p.SeasonId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.User).WithMany().HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.Match).WithMany(m => m.Predictions).HasForeignKey(p => p.MatchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.PredictedTeam).WithMany().HasForeignKey(p => p.PredictedTeamId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.PredictedPlayer).WithMany().HasForeignKey(p => p.PredictedPlayerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(p => new { p.SeasonId, p.UserId, p.MatchId }).IsUnique().HasFilter("match_id IS NOT NULL");
        builder.HasIndex(p => new { p.SeasonId, p.UserId, p.PredictionType }).IsUnique().HasFilter("match_id IS NULL");
    }
}
