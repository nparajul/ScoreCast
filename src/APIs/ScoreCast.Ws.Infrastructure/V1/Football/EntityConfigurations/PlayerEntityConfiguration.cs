using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScoreCast.Ws.Domain.V1.Entities.Football;
using ScoreCast.Ws.Infrastructure.V1.Shared;

namespace ScoreCast.Ws.Infrastructure.V1.Football.EntityConfigurations;

internal sealed class PlayerEntityConfiguration : BaseEntityConfiguration<Player>
{
    public override void Configure(EntityTypeBuilder<Player> builder)
    {
        base.Configure(builder);
        builder.ToTable("player");
        builder.HasKey(p => p.Id);

        var order = 1;
        builder.Property(p => p.Name).HasColumnName("name").HasColumnOrder(order++).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Position).HasColumnName("position").HasColumnOrder(order++).HasMaxLength(50);
        builder.Property(p => p.DateOfBirth).HasColumnName("date_of_birth").HasColumnOrder(order++);
        builder.Property(p => p.Nationality).HasColumnName("nationality").HasColumnOrder(order++).HasMaxLength(100);
        builder.Property(p => p.PhotoUrl).HasColumnName("photo_url").HasColumnOrder(order++).HasMaxLength(500);
        builder.Property(p => p.ExternalId).HasColumnName("external_id").HasColumnOrder(order++).HasMaxLength(50);

        builder.HasIndex(p => p.ExternalId).IsUnique();
    }
}
