using FluentValidation;

namespace ScoreCast.Web.Validation;

public static class FluentValidatorExtensions
{
    public static Func<object, string, Task<IEnumerable<string>>> ToMudValidation<T>(this AbstractValidator<T> validator)
    {
        return async (model, propertyName) =>
        {
            var result = await validator.ValidateAsync(
                ValidationContext<T>.CreateWithOptions((T)model, opt => opt.IncludeProperties(propertyName)));
            return result.IsValid ? [] : result.Errors.Select(e => e.ErrorMessage);
        };
    }
}
