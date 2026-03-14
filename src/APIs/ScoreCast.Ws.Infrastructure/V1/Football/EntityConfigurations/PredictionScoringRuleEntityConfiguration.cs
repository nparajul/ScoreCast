using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScoreCast.Ws.Domain.V1.Entities.Football;
using ScoreCast.Ws.Infrastructure.V1.Shared;

namespace ScoreCast.Ws.Infrastructure.V1.Football.EntityConfigurations;

internal sealed class PredictionScoringRuleEntityConfiguration : BaseEntityConfiguration<PredictionScoringRule>
{
    public override void Configure(EntityTypeBuilder<PredictionScoringRule> builder)
    {
        base.Configure(builder);
        builder.ToTable("prediction_scoring_rule");
        builder.HasKey(p => p.Id);
        var order = 1;

        builder.Property(p => p.Outcome).HasColumnName("outcome").HasColumnOrder(order++).IsRequired().HasConversion<string>().HasMaxLength(50);
        builder.Property(p => p.Points).HasColumnName("points").HasColumnOrder(order++).IsRequired();
        builder.Property(p => p.Description).HasColumnName("description").HasColumnOrder(order++).IsRequired().HasMaxLength(200);
        builder.Property(p => p.DisplayOrder).HasColumnName("display_order").HasColumnOrder(order++).IsRequired();

        builder.HasIndex(p => p.Outcome).IsUnique();
    }
}
