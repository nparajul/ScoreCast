using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScoreCast.Shared.Enums;
using ScoreCast.Ws.Domain.V1.Entities.Football;
using ScoreCast.Ws.Infrastructure.V1.Shared;

namespace ScoreCast.Ws.Infrastructure.V1.Football.EntityConfigurations;

internal sealed class StageEntityConfiguration : BaseEntityConfiguration<Stage>
{
    public override void Configure(EntityTypeBuilder<Stage> builder)
    {
        base.Configure(builder);
        builder.ToTable("stage");
        builder.HasKey(s => s.Id);
        var order = 1;

        builder.Property(s => s.SeasonId).HasColumnName("season_id").HasColumnOrder(order++).IsRequired();
        builder.Property(s => s.Name).HasColumnName("name").HasColumnOrder(order++).IsRequired().HasMaxLength(100);
        builder.Property(s => s.StageType).HasColumnName("stage_type").HasColumnOrder(order++).IsRequired().HasConversion<string>().HasMaxLength(20).HasDefaultValue(StageType.League);
        builder.Property(s => s.SortOrder).HasColumnName("sort_order").HasColumnOrder(order++).HasDefaultValue(0);
        builder.Property(s => s.IsActive).HasColumnName("is_active").HasColumnOrder(order++).HasDefaultValue(true);

        builder.HasOne(s => s.Season).WithMany(se => se.Stages).HasForeignKey(s => s.SeasonId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(s => new { s.SeasonId, s.Name }).IsUnique();
    }
}
