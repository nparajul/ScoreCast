using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScoreCast.Ws.Domain.V1.Entities.Football;
using ScoreCast.Ws.Infrastructure.V1.Shared;

namespace ScoreCast.Ws.Infrastructure.V1.Football.EntityConfigurations;

internal sealed class CoachEntityConfiguration : BaseEntityConfiguration<Coach>
{
    public override void Configure(EntityTypeBuilder<Coach> builder)
    {
        base.Configure(builder);
        builder.ToTable("coach");
        builder.HasKey(c => c.Id);

        var order = 1;
        builder.Property(c => c.Name).HasColumnName("name").HasColumnOrder(order++).IsRequired().HasMaxLength(200);
        builder.Property(c => c.DateOfBirth).HasColumnName("date_of_birth").HasColumnOrder(order++);
        builder.Property(c => c.Nationality).HasColumnName("nationality").HasColumnOrder(order++).HasMaxLength(100);
        builder.Property(c => c.PhotoUrl).HasColumnName("photo_url").HasColumnOrder(order++).HasMaxLength(500);
        builder.Property(c => c.ExternalId).HasColumnName("external_id").HasColumnOrder(order++).HasMaxLength(50);

        builder.HasIndex(c => c.ExternalId).IsUnique();
    }
}
