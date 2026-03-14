using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScoreCast.Ws.Domain.V1.Entities.Football;
using ScoreCast.Shared.Enums;
using ScoreCast.Ws.Infrastructure.V1.Shared;

namespace ScoreCast.Ws.Infrastructure.V1.Football.EntityConfigurations;

internal sealed class CompetitionEntityConfiguration : BaseEntityConfiguration<Competition>
{
    public override void Configure(EntityTypeBuilder<Competition> builder)
    {
        base.Configure(builder);
        builder.ToTable("competition");
        builder.HasKey(c => c.Id);
        var order = 1;

        builder.Property(c => c.Name).HasColumnName("name").HasColumnOrder(order++).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Code).HasColumnName("code").HasColumnOrder(order++).IsRequired().HasMaxLength(20);
        builder.Property(c => c.CountryId).HasColumnName("country_id").HasColumnOrder(order++).IsRequired();
        builder.Property(c => c.LogoUrl).HasColumnName("logo_url").HasColumnOrder(order++).HasMaxLength(500);
        builder.Property(c => c.ExternalId).HasColumnName("external_id").HasColumnOrder(order++).HasMaxLength(50);
        builder.Property(c => c.Type).HasColumnName("type").HasColumnOrder(order++).HasConversion<string>().HasMaxLength(20).HasDefaultValue(LeagueType.League);
        builder.Property(c => c.SortOrder).HasColumnName("sort_order").HasColumnOrder(order++).HasDefaultValue(0);
        builder.Property(c => c.IsActive).HasColumnName("is_active").HasColumnOrder(order++).HasDefaultValue(true);

        builder.HasOne(c => c.Country).WithMany().HasForeignKey(c => c.CountryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(c => c.Name).IsUnique();
        builder.HasIndex(c => c.Code).IsUnique();
        builder.HasIndex(c => c.ExternalId).IsUnique().HasFilter("external_id IS NOT NULL");
    }
}
