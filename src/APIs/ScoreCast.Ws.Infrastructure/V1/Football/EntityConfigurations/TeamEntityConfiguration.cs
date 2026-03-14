using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScoreCast.Ws.Domain.V1.Entities.Football;
using ScoreCast.Ws.Infrastructure.V1.Shared;

namespace ScoreCast.Ws.Infrastructure.V1.Football.EntityConfigurations;

internal sealed class TeamEntityConfiguration : BaseEntityConfiguration<Team>
{
    public override void Configure(EntityTypeBuilder<Team> builder)
    {
        base.Configure(builder);
        builder.ToTable("team");
        builder.HasKey(t => t.Id);
        var order = 1;

        builder.Property(t => t.Name).HasColumnName("name").HasColumnOrder(order++).IsRequired().HasMaxLength(200);
        builder.Property(t => t.ShortName).HasColumnName("short_name").HasColumnOrder(order++).HasMaxLength(50);
        builder.Property(t => t.LogoUrl).HasColumnName("logo_url").HasColumnOrder(order++).HasMaxLength(500);
        builder.Property(t => t.ExternalId).HasColumnName("external_id").HasColumnOrder(order++).HasMaxLength(50);
        builder.Property(t => t.CountryId).HasColumnName("country_id").HasColumnOrder(order++).IsRequired();
        builder.Property(t => t.Founded).HasColumnName("founded").HasColumnOrder(order++);
        builder.Property(t => t.Venue).HasColumnName("venue").HasColumnOrder(order++).HasMaxLength(200);
        builder.Property(t => t.ClubColors).HasColumnName("club_colors").HasColumnOrder(order++).HasMaxLength(100);
        builder.Property(t => t.Website).HasColumnName("website").HasColumnOrder(order++).HasMaxLength(500);
        builder.Property(t => t.IsActive).HasColumnName("is_active").HasColumnOrder(order++).HasDefaultValue(true);

        builder.HasOne(t => t.Country).WithMany().HasForeignKey(t => t.CountryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(t => t.Name).IsUnique();
        builder.HasIndex(t => t.ExternalId).IsUnique().HasFilter("external_id IS NOT NULL");
    }
}
