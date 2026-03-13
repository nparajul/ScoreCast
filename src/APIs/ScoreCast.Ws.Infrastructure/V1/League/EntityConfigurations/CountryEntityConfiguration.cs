using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScoreCast.Ws.Domain.V1.Entities.League;
using ScoreCast.Ws.Infrastructure.V1.Shared;

namespace ScoreCast.Ws.Infrastructure.V1.League.EntityConfigurations;

internal sealed class CountryEntityConfiguration : BaseEntityConfiguration<CountryMaster>
{
    public override void Configure(EntityTypeBuilder<CountryMaster> builder)
    {
        base.Configure(builder);

        builder.ToTable("country_master");

        builder.HasKey(c => c.Id);

        var order = 1;

        builder.Property(c => c.Name)
            .HasColumnName("name")
            .HasColumnOrder(order++)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Code)
            .HasColumnName("code")
            .HasColumnOrder(order++)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(c => c.FlagUrl)
            .HasColumnName("flag_url")
            .HasColumnOrder(order++)
            .HasMaxLength(500);

        builder.Property(c => c.IsActive)
            .HasColumnName("is_active")
            .HasColumnOrder(order++)
            .HasDefaultValue(true);

        builder.HasIndex(c => c.Name).IsUnique();
        builder.HasIndex(c => c.Code).IsUnique();
    }
}
