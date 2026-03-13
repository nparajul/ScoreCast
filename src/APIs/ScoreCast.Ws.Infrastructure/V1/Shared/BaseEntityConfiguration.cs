using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScoreCast.Ws.Domain.V1.Entities.Common;
using ScoreCast.Ws.Infrastructure.V1.Shared.Converters;

namespace ScoreCast.Ws.Infrastructure.V1.Shared;

internal abstract class BaseEntityConfiguration<T> : IEntityTypeConfiguration<T> where T : ScoreCastEntity
{
    public virtual void Configure(EntityTypeBuilder<T> builder)
    {
        builder.Property(p => p.Id)
            .HasColumnName("id")
            .HasColumnOrder(0)
            .UseIdentityByDefaultColumn();

        builder.Property(p => p.CreatedBy)
            .HasColumnName("created_by")
            .IsRequired()
            .HasDefaultValueSql("current_user")
            .HasColumnOrder(991)
            .HasMaxLength(50);

        builder.Property(p => p.CreatedDate)
            .HasColumnName("created_date")
            .HasConversion<ScoreCastDateTimeConverter>()
            .HasDefaultValueSql("now()")
            .HasColumnOrder(992)
            .IsRequired();

        builder.Property(p => p.CreatedByApp)
            .HasColumnName("created_by_app")
            .IsRequired()
            .HasColumnOrder(993)
            .HasMaxLength(100);

        builder.Property(p => p.ModifiedBy)
            .HasColumnName("modified_by")
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValueSql("current_user")
            .HasColumnOrder(995);

        builder.Property(p => p.ModifiedDate)
            .HasColumnName("modified_date")
            .HasConversion<ScoreCastDateTimeConverter>()
            .HasDefaultValueSql("now()")
            .HasColumnOrder(996)
            .IsRequired();

        builder.Property(p => p.ModifiedByApp)
            .HasColumnName("modified_by_app")
            .HasColumnOrder(997)
            .HasMaxLength(100);

        builder.Property(p => p.IsDeleted)
            .HasColumnName("is_deleted")
            .HasColumnOrder(999)
            .HasDefaultValue(false)
            .IsRequired();

        builder.HasQueryFilter(q => !q.IsDeleted);
    }
}
