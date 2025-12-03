using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScoreCast.Ws.Domain.V1.Entities;
using ScoreCast.Ws.Infrastructure.V1.Shared;

namespace ScoreCast.Ws.Infrastructure.V1.Shared.EntityConfigurations;

internal sealed class ExternalMappingEntityConfiguration : BaseEntityConfiguration<ExternalMapping>
{
    public override void Configure(EntityTypeBuilder<ExternalMapping> builder)
    {
        base.Configure(builder);
        builder.ToTable("external_mapping");
        builder.HasKey(e => e.Id);
        var order = 1;

        builder.Property(e => e.EntityType).HasColumnName("entity_type").HasColumnOrder(order++).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(e => e.EntityId).HasColumnName("entity_id").HasColumnOrder(order++).IsRequired();
        builder.Property(e => e.Source).HasColumnName("source").HasColumnOrder(order++).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(e => e.ExternalCode).HasColumnName("external_code").HasColumnOrder(order++).HasMaxLength(50).IsRequired();

        builder.HasIndex(e => new { e.EntityType, e.EntityId, e.Source }).IsUnique();
        builder.HasIndex(e => new { e.Source, e.EntityType, e.ExternalCode });
    }
}
