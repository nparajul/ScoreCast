using FluentValidation;
using ScoreCast.Web.ViewModels.Auth;

namespace ScoreCast.Web.Validation.Auth;

public sealed class RegisterViewModelValidator : AbstractValidator<RegisterViewModel>
{
    public RegisterViewModelValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Display name is required")
            .MaximumLength(30).WithMessage("Display name must be 30 characters or less");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("At least 8 characters")
            .MaximumLength(128).WithMessage("128 characters or less")
            .Matches("[A-Z]").WithMessage("At least 1 uppercase letter")
            .Matches("[a-z]").WithMessage("At least 1 lowercase letter")
            .Matches("[0-9]").WithMessage("At least 1 digit")
            .Matches(@"[^a-zA-Z0-9]").WithMessage("At least 1 special character");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Please confirm your password")
            .Equal(x => x.Password).WithMessage("Passwords do not match");
    }
}
