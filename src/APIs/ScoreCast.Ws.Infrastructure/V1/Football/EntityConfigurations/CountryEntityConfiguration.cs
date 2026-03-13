using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScoreCast.Ws.Domain.V1.Entities.Football;
using ScoreCast.Ws.Infrastructure.V1.Shared;

namespace ScoreCast.Ws.Infrastructure.V1.Football.EntityConfigurations;

internal sealed class CountryEntityConfiguration : BaseEntityConfiguration<Country>
{
    public override void Configure(EntityTypeBuilder<Country> builder)
    {
        base.Configure(builder);
        builder.ToTable("country");
        builder.HasKey(c => c.Id);
        var order = 1;

        builder.Property(c => c.Name).HasColumnName("name").HasColumnOrder(order++).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Code).HasColumnName("code").HasColumnOrder(order++).IsRequired().HasMaxLength(10);
        builder.Property(c => c.ExternalId).HasColumnName("external_id").HasColumnOrder(order++).HasMaxLength(50);
        builder.Property(c => c.FlagUrl).HasColumnName("flag_url").HasColumnOrder(order++).HasMaxLength(500);
        builder.Property(c => c.IsActive).HasColumnName("is_active").HasColumnOrder(order++).HasDefaultValue(true);

        builder.HasIndex(c => c.Name).IsUnique();
        builder.HasIndex(c => c.Code).IsUnique();
        builder.HasIndex(c => c.ExternalId).IsUnique().HasFilter("external_id IS NOT NULL");
    }
}
