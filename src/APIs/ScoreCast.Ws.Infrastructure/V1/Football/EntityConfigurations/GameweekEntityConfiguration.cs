using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScoreCast.Ws.Domain.V1.Entities.Football;
using ScoreCast.Shared.Enums;
using ScoreCast.Ws.Infrastructure.V1.Shared;

namespace ScoreCast.Ws.Infrastructure.V1.Football.EntityConfigurations;

internal sealed class GameweekEntityConfiguration : BaseEntityConfiguration<Gameweek>
{
    public override void Configure(EntityTypeBuilder<Gameweek> builder)
    {
        base.Configure(builder);
        builder.ToTable("gameweek");
        builder.HasKey(g => g.Id);
        var order = 1;

        builder.Property(g => g.SeasonId).HasColumnName("season_id").HasColumnOrder(order++).IsRequired();
        builder.Property(g => g.StageId).HasColumnName("stage_id").HasColumnOrder(order++);
        builder.Property(g => g.Number).HasColumnName("number").HasColumnOrder(order++).IsRequired();
        builder.Property(g => g.StartDate).HasColumnName("start_date").HasColumnOrder(order++);
        builder.Property(g => g.EndDate).HasColumnName("end_date").HasColumnOrder(order++);
        builder.Property(g => g.Status).HasColumnName("status").HasColumnOrder(order++).HasConversion<string>().HasMaxLength(20).HasDefaultValue(GameweekStatus.Upcoming);

        builder.HasOne(g => g.Season).WithMany(s => s.Gameweeks).HasForeignKey(g => g.SeasonId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(g => g.Stage).WithMany(s => s.Gameweeks).HasForeignKey(g => g.StageId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(g => new { g.SeasonId, g.Number }).IsUnique();
    }
}
