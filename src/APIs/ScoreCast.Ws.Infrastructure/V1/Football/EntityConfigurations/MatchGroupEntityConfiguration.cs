using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScoreCast.Ws.Domain.V1.Entities.Football;
using ScoreCast.Ws.Infrastructure.V1.Shared;

namespace ScoreCast.Ws.Infrastructure.V1.Football.EntityConfigurations;

internal sealed class MatchGroupEntityConfiguration : BaseEntityConfiguration<MatchGroup>
{
    public override void Configure(EntityTypeBuilder<MatchGroup> builder)
    {
        base.Configure(builder);
        builder.ToTable("match_group");
        builder.HasKey(g => g.Id);
        var order = 1;

        builder.Property(g => g.StageId).HasColumnName("stage_id").HasColumnOrder(order++).IsRequired();
        builder.Property(g => g.Name).HasColumnName("name").HasColumnOrder(order++).IsRequired().HasMaxLength(100);
        builder.Property(g => g.SortOrder).HasColumnName("sort_order").HasColumnOrder(order++).HasDefaultValue(0);

        builder.HasOne(g => g.Stage).WithMany(s => s.MatchGroups).HasForeignKey(g => g.StageId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(g => new { g.StageId, g.Name }).IsUnique();
    }
}
