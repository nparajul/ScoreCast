using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScoreCast.Ws.Domain.V1.Entities.UserManagement;
using ScoreCast.Ws.Infrastructure.V1.Shared;

namespace ScoreCast.Ws.Infrastructure.V1.UserManagement.EntityConfigurations;

internal sealed class RoleMasterEntityConfiguration : BaseEntityConfiguration<RoleMaster>
{
    public override void Configure(EntityTypeBuilder<RoleMaster> builder)
    {
        base.Configure(builder);
        builder.ToTable("role_master");
        builder.HasKey(r => r.Id);

        var order = 1;
        builder.Property(r => r.Name).HasColumnName("name").HasColumnOrder(order++).IsRequired().HasMaxLength(50);
        builder.Property(r => r.Description).HasColumnName("description").HasColumnOrder(order++).HasMaxLength(200);
        builder.Property(r => r.SortOrder).HasColumnName("sort_order").HasColumnOrder(order++).HasDefaultValue(0);
        builder.Property(r => r.IsActive).HasColumnName("is_active").HasColumnOrder(order++).HasDefaultValue(true);

        builder.HasIndex(r => r.Name).IsUnique();
    }
}
