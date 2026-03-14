using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScoreCast.Ws.Domain.V1.Entities.UserManagement;
using ScoreCast.Ws.Infrastructure.V1.Shared;

namespace ScoreCast.Ws.Infrastructure.V1.UserManagement.EntityConfigurations;

internal sealed class RolePageEntityConfiguration : BaseEntityConfiguration<RolePage>
{
    public override void Configure(EntityTypeBuilder<RolePage> builder)
    {
        base.Configure(builder);
        builder.ToTable("role_page");
        builder.HasKey(rp => rp.Id);

        var order = 1;
        builder.Property(rp => rp.RoleId).HasColumnName("role_id").HasColumnOrder(order++).IsRequired();
        builder.Property(rp => rp.PageId).HasColumnName("page_id").HasColumnOrder(order++).IsRequired();

        builder.HasOne(rp => rp.Role).WithMany(r => r.RolePages).HasForeignKey(rp => rp.RoleId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(rp => rp.Page).WithMany(p => p.RolePages).HasForeignKey(rp => rp.PageId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(rp => new { rp.RoleId, rp.PageId }).IsUnique();
    }
}
