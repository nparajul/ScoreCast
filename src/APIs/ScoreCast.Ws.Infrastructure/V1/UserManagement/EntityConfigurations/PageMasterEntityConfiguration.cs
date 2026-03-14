using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScoreCast.Ws.Domain.V1.Entities.UserManagement;
using ScoreCast.Ws.Infrastructure.V1.Shared;

namespace ScoreCast.Ws.Infrastructure.V1.UserManagement.EntityConfigurations;

internal sealed class PageMasterEntityConfiguration : BaseEntityConfiguration<PageMaster>
{
    public override void Configure(EntityTypeBuilder<PageMaster> builder)
    {
        base.Configure(builder);
        builder.ToTable("page_master");
        builder.HasKey(p => p.Id);

        var order = 1;
        builder.Property(p => p.PageCode).HasColumnName("page_code").HasColumnOrder(order++).IsRequired().HasMaxLength(50);
        builder.Property(p => p.PageName).HasColumnName("page_name").HasColumnOrder(order++).IsRequired().HasMaxLength(100);
        builder.Property(p => p.PageUrl).HasColumnName("page_url").HasColumnOrder(order++).HasMaxLength(500);
        builder.Property(p => p.ParentPageId).HasColumnName("parent_page_id").HasColumnOrder(order++);

        builder.HasIndex(p => p.PageCode).IsUnique();

        builder.HasOne(p => p.ParentPage)
            .WithMany(p => p.ChildPages)
            .HasForeignKey(p => p.ParentPageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
