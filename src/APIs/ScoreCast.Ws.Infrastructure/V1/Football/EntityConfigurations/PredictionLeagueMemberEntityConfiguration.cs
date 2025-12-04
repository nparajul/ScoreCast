using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ScoreCast.Ws.Domain.V1.Entities.Football;
using ScoreCast.Ws.Infrastructure.V1.Shared;

namespace ScoreCast.Ws.Infrastructure.V1.Football.EntityConfigurations;

internal sealed class PredictionLeagueMemberEntityConfiguration : BaseEntityConfiguration<PredictionLeagueMember>
{
    public override void Configure(EntityTypeBuilder<PredictionLeagueMember> builder)
    {
        base.Configure(builder);
        builder.ToTable("prediction_league_member");
        builder.HasKey(p => p.Id);
        var order = 1;

        builder.Property(p => p.PredictionLeagueId).HasColumnName("prediction_league_id").HasColumnOrder(order++).IsRequired();
        builder.Property(p => p.UserId).HasColumnName("user_id").HasColumnOrder(order++).IsRequired();
        builder.Property(p => p.Role).HasColumnName("role").HasColumnOrder(order++).IsRequired().HasConversion<string>().HasMaxLength(20);

        builder.HasOne(p => p.PredictionLeague).WithMany(l => l.Members).HasForeignKey(p => p.PredictionLeagueId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(p => p.User).WithMany().HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(p => new { p.PredictionLeagueId, p.UserId }).IsUnique();
    }
}
