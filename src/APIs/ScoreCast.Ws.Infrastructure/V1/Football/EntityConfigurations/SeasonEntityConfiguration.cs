using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScoreCast.Ws.Domain.V1.Entities.Football;
using ScoreCast.Ws.Infrastructure.V1.Shared;

namespace ScoreCast.Ws.Infrastructure.V1.Football.EntityConfigurations;

internal sealed class SeasonEntityConfiguration : BaseEntityConfiguration<Season>
{
    public override void Configure(EntityTypeBuilder<Season> builder)
    {
        base.Configure(builder);
        builder.ToTable("season");
        builder.HasKey(s => s.Id);
        var order = 1;

        builder.Property(s => s.Name).HasColumnName("name").HasColumnOrder(order++).IsRequired().HasMaxLength(50);
        builder.Property(s => s.CompetitionId).HasColumnName("competition_id").HasColumnOrder(order++).IsRequired();
        builder.Property(s => s.ExternalId).HasColumnName("external_id").HasColumnOrder(order++).HasMaxLength(50);
        builder.Property(s => s.StartDate).HasColumnName("start_date").HasColumnOrder(order++);
        builder.Property(s => s.EndDate).HasColumnName("end_date").HasColumnOrder(order++);
        builder.Property(s => s.CurrentMatchday).HasColumnName("current_matchday").HasColumnOrder(order++);
        builder.Property(s => s.WinnerTeamId).HasColumnName("winner_team_id").HasColumnOrder(order++);
        builder.Property(s => s.IsCurrent).HasColumnName("is_current").HasColumnOrder(order++).HasDefaultValue(false);

        builder.HasOne(s => s.Competition).WithMany(c => c.Seasons).HasForeignKey(s => s.CompetitionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.WinnerTeam).WithMany().HasForeignKey(s => s.WinnerTeamId).OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(s => new { s.CompetitionId, s.Name }).IsUnique();
        builder.HasIndex(s => s.ExternalId).IsUnique().HasFilter("external_id IS NOT NULL");
    }
}
