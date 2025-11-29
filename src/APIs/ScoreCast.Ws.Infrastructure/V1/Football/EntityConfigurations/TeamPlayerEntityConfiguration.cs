using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScoreCast.Ws.Domain.V1.Entities.Football;
using ScoreCast.Ws.Infrastructure.V1.Shared;

namespace ScoreCast.Ws.Infrastructure.V1.Football.EntityConfigurations;

internal sealed class TeamPlayerEntityConfiguration : BaseEntityConfiguration<TeamPlayer>
{
    public override void Configure(EntityTypeBuilder<TeamPlayer> builder)
    {
        base.Configure(builder);
        builder.ToTable("team_player");
        builder.HasKey(tp => tp.Id);

        builder.Property(tp => tp.TeamId).HasColumnName("team_id").HasColumnOrder(1).IsRequired();
        builder.Property(tp => tp.PlayerId).HasColumnName("player_id").HasColumnOrder(2).IsRequired();
        builder.Property(tp => tp.SeasonId).HasColumnName("season_id").HasColumnOrder(3).IsRequired();

        builder.HasOne(tp => tp.Team).WithMany(t => t.TeamPlayers).HasForeignKey(tp => tp.TeamId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(tp => tp.Player).WithMany(p => p.TeamPlayers).HasForeignKey(tp => tp.PlayerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(tp => tp.Season).WithMany(s => s.TeamPlayers).HasForeignKey(tp => tp.SeasonId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(tp => new { tp.TeamId, tp.PlayerId, tp.SeasonId }).IsUnique();
    }
}
