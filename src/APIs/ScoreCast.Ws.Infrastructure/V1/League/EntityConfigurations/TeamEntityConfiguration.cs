using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScoreCast.Ws.Domain.V1.Entities.League;
using ScoreCast.Ws.Infrastructure.V1.Shared;

namespace ScoreCast.Ws.Infrastructure.V1.League.EntityConfigurations;

internal sealed class TeamEntityConfiguration : BaseEntityConfiguration<TeamMaster>
{
    public override void Configure(EntityTypeBuilder<TeamMaster> builder)
    {
        base.Configure(builder);

        builder.ToTable("team_master");

        builder.HasKey(t => t.Id);

        var order = 1;

        builder.Property(t => t.Name)
            .HasColumnName("name")
            .HasColumnOrder(order++)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.ShortName)
            .HasColumnName("short_name")
            .HasColumnOrder(order++)
            .HasMaxLength(10);

        builder.Property(t => t.LogoUrl)
            .HasColumnName("logo_url")
            .HasColumnOrder(order++)
            .HasMaxLength(500);

        builder.Property(t => t.LeagueId)
            .HasColumnName("league_id")
            .HasColumnOrder(order++)
            .IsRequired();

        builder.Property(t => t.CountryId)
            .HasColumnName("country_id")
            .HasColumnOrder(order++)
            .IsRequired();

        builder.Property(t => t.IsActive)
            .HasColumnName("is_active")
            .HasColumnOrder(order++)
            .HasDefaultValue(true);

        builder.HasOne(t => t.League)
            .WithMany(l => l.Teams)
            .HasForeignKey(t => t.LeagueId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Country)
            .WithMany()
            .HasForeignKey(t => t.CountryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => new { t.Name, t.LeagueId }).IsUnique();
    }
}
