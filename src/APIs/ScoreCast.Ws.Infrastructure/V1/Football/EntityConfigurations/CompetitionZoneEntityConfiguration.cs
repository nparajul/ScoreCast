using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScoreCast.Ws.Domain.V1.Entities.Football;
using ScoreCast.Ws.Infrastructure.V1.Shared;

namespace ScoreCast.Ws.Infrastructure.V1.Football.EntityConfigurations;

internal sealed class CompetitionZoneEntityConfiguration : BaseEntityConfiguration<CompetitionZone>
{
    public override void Configure(EntityTypeBuilder<CompetitionZone> builder)
    {
        base.Configure(builder);
        builder.ToTable("competition_zone");
        builder.HasKey(z => z.Id);
        var order = 1;

        builder.Property(z => z.CompetitionId).HasColumnName("competition_id").HasColumnOrder(order++).IsRequired();
        builder.Property(z => z.Name).HasColumnName("name").HasColumnOrder(order++).IsRequired().HasMaxLength(100);
        builder.Property(z => z.Color).HasColumnName("color").HasColumnOrder(order++).IsRequired().HasMaxLength(20);
        builder.Property(z => z.StartPosition).HasColumnName("start_position").HasColumnOrder(order++);
        builder.Property(z => z.EndPosition).HasColumnName("end_position").HasColumnOrder(order++);
        builder.Property(z => z.SortOrder).HasColumnName("sort_order").HasColumnOrder(order++).HasDefaultValue(0);

        builder.HasOne(z => z.Competition).WithMany().HasForeignKey(z => z.CompetitionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(z => new { z.CompetitionId, z.StartPosition }).IsUnique();
    }
}
