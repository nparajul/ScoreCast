using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScoreCast.Ws.Domain.V1.Entities.Football;
using ScoreCast.Ws.Infrastructure.V1.Shared;

namespace ScoreCast.Ws.Infrastructure.V1.Football.EntityConfigurations;

internal sealed class SeasonTeamEntityConfiguration : BaseEntityConfiguration<SeasonTeam>
{
    public override void Configure(EntityTypeBuilder<SeasonTeam> builder)
    {
        base.Configure(builder);
        builder.ToTable("season_team");
        builder.HasKey(st => st.Id);

        builder.Property(st => st.SeasonId).HasColumnName("season_id").HasColumnOrder(1).IsRequired();
        builder.Property(st => st.TeamId).HasColumnName("team_id").HasColumnOrder(2).IsRequired();

        builder.HasOne(st => st.Season).WithMany(s => s.SeasonTeams).HasForeignKey(st => st.SeasonId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(st => st.Team).WithMany(t => t.SeasonTeams).HasForeignKey(st => st.TeamId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(st => new { st.SeasonId, st.TeamId }).IsUnique();
    }
}
