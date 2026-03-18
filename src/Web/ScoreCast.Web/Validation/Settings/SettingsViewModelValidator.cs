using FluentValidation;
using ScoreCast.Web.ViewModels.Settings;

namespace ScoreCast.Web.Validation.Settings;

public sealed class SettingsViewModelValidator : AbstractValidator<SettingsViewModel>
{
    public SettingsViewModelValidator()
    {
        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Display name is required")
            .MaximumLength(30).WithMessage("Display name must be 30 characters or less");

        RuleFor(x => x.FavoriteTeam)
            .MaximumLength(50).WithMessage("Favourite team must be 50 characters or less");
    }
}
