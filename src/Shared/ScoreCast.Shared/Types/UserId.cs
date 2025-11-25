using ScoreCast.Shared.Extensions;

namespace ScoreCast.Shared.Types;

public sealed record UserId
{
    public UserId(string? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var userId = value.ToTrimUpper();

        if (userId.Contains('\\'))
        {
            var splitValues = userId.Split('\\');
            userId = splitValues[^1];
        }
        else
        {
            userId = userId.Contains('@') ? userId.Split("@")[0] : userId;
        }

        Value = userId.Trim();
    }

    private string Value { get; }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator string(UserId x)
    {
        return x.Value;
    }

    public static UserId Create(string? userId)
    {
        return new UserId(userId);
    }
}
