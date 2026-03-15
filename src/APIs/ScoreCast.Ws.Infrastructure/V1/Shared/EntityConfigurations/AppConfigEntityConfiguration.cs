using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScoreCast.Ws.Domain.V1.Entities;

namespace ScoreCast.Ws.Infrastructure.V1.Shared.EntityConfigurations;

internal sealed class AppConfigEntityConfiguration : BaseEntityConfiguration<AppConfig>
{
    public override void Configure(EntityTypeBuilder<AppConfig> builder)
    {
        base.Configure(builder);
        builder.ToTable("app_config");
        builder.HasKey(a => a.Id);
        var order = 1;

        builder.Property(a => a.Key).HasColumnName("key").HasColumnOrder(order++).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Value).HasColumnName("value").HasColumnOrder(order++).HasColumnType("jsonb").IsRequired();

        builder.HasIndex(a => a.Key).IsUnique();
    }
}
