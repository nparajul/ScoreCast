using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScoreCast.Ws.Domain.V1.Entities.Football;
using ScoreCast.Ws.Infrastructure.V1.Shared;

namespace ScoreCast.Ws.Infrastructure.V1.Football.EntityConfigurations;

internal sealed class MatchHighlightEntityConfiguration : BaseEntityConfiguration<MatchHighlight>
{
    public override void Configure(EntityTypeBuilder<MatchHighlight> builder)
    {
        base.Configure(builder);
        builder.ToTable("match_highlight");
        builder.HasKey(m => m.Id);
        var order = 1;

        builder.Property(m => m.MatchId).HasColumnName("match_id").HasColumnOrder(order++).IsRequired();
        builder.Property(m => m.Title).HasColumnName("title").HasColumnOrder(order++).IsRequired();
        builder.Property(m => m.EmbedHtml).HasColumnName("embed_html").HasColumnOrder(order++).IsRequired();
        builder.Property(m => m.Type).HasColumnName("type").HasColumnOrder(order).HasConversion<string>().HasDefaultValue(ScoreCast.Shared.Enums.HighlightType.Highlight).IsRequired();

        builder.HasIndex(m => new { m.MatchId, m.Type }).IsUnique();
        builder.HasOne(m => m.Match).WithMany().HasForeignKey(m => m.MatchId);
    }
}
