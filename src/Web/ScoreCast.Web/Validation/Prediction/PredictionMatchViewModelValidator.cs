using FluentValidation;
using ScoreCast.Web.ViewModels;

namespace ScoreCast.Web.Validation.Prediction;

public sealed class PredictionMatchViewModelValidator : AbstractValidator<PredictionMatchViewModel>
{
    public PredictionMatchViewModelValidator()
    {
        When(x => !x.IsLocked, () =>
        {
            RuleFor(x => x.PredictedHomeScore)
                .GreaterThanOrEqualTo(0).When(x => x.PredictedHomeScore.HasValue)
                .WithMessage("Score cannot be negative");

            RuleFor(x => x.PredictedAwayScore)
                .GreaterThanOrEqualTo(0).When(x => x.PredictedAwayScore.HasValue)
                .WithMessage("Score cannot be negative");

            RuleFor(x => x)
                .Must(x => x.PredictedHomeScore.HasValue == x.PredictedAwayScore.HasValue)
                .WithMessage(x => $"Enter both scores for {x.HomeTeamShortName} vs {x.AwayTeamShortName}");
        });
    }
}
