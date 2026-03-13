using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScoreCast.Ws.Domain.V1.Entities.League;
using ScoreCast.Ws.Infrastructure.V1.Shared;

namespace ScoreCast.Ws.Infrastructure.V1.League.EntityConfigurations;

internal sealed class LeagueEntityConfiguration : BaseEntityConfiguration<LeagueMaster>
{
    public override void Configure(EntityTypeBuilder<LeagueMaster> builder)
    {
        base.Configure(builder);

        builder.ToTable("league_master");

        builder.HasKey(l => l.Id);

        var order = 1;

        builder.Property(l => l.Name)
            .HasColumnName("name")
            .HasColumnOrder(order++)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(l => l.CountryId)
            .HasColumnName("country_id")
            .HasColumnOrder(order++)
            .IsRequired();

        builder.Property(l => l.LogoUrl)
            .HasColumnName("logo_url")
            .HasColumnOrder(order++)
            .HasMaxLength(500);

        builder.Property(l => l.SortOrder)
            .HasColumnName("sort_order")
            .HasColumnOrder(order++)
            .HasDefaultValue(0);

        builder.Property(l => l.IsActive)
            .HasColumnName("is_active")
            .HasColumnOrder(order++)
            .HasDefaultValue(true);

        builder.HasOne(l => l.Country)
            .WithMany()
            .HasForeignKey(l => l.CountryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => l.Name).IsUnique();
    }
}
