using System.Globalization;
using System.Text.Json.Serialization;

namespace ScoreCast.Shared.Types;

[JsonConverter(typeof(ScoreCastDateTimeJsonConverter))]
public sealed record ScoreCastDateTime
{
    private const string DefaultFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";

    private ScoreCastDateTime()
    {
        Value = DateTime.UtcNow;
    }

    public ScoreCastDateTime(DateTime dateTimeValue)
    {
        Value = dateTimeValue.Kind != DateTimeKind.Utc ? dateTimeValue.ToUniversalTime() : dateTimeValue;
    }

    public ScoreCastDateTime(DateTimeOffset dateTimeOffsetValue)
    {
        Value = dateTimeOffsetValue.UtcDateTime;
    }

    public ScoreCastDateTime(string? stringDateTimeValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stringDateTimeValue);

        if (DateTime.TryParse(stringDateTimeValue, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var dtValue))
        {
            Value = dtValue;
        }
        else
        {
            throw new ArgumentException("Invalid date time format", nameof(stringDateTimeValue));
        }
    }

    public DateTime Value { get; }

    public DateOnly Date => DateOnly.FromDateTime(Value);

    public TimeOnly Time => TimeOnly.FromDateTime(Value);

    public static ScoreCastDateTime Now => new(DateTime.UtcNow);

    public static ScoreCastDateTime MinValue => new(DateTime.MinValue);

    public static ScoreCastDateTime MaxValue => new(DateTime.MaxValue);

    public static ScoreCastDateTime operator +(ScoreCastDateTime dateTime, TimeSpan timeSpan) =>
        new(dateTime.Value + timeSpan);

    public static bool operator >(ScoreCastDateTime left, ScoreCastDateTime right) => left.Value > right.Value;

    public static bool operator <(ScoreCastDateTime left, ScoreCastDateTime right) => left.Value < right.Value;

    public static bool operator >=(ScoreCastDateTime left, ScoreCastDateTime right) => left.Value >= right.Value;

    public static bool operator <=(ScoreCastDateTime left, ScoreCastDateTime right) => left.Value <= right.Value;

    public override string ToString() => Value.ToString(DefaultFormat);

    public string ToString(string format) => Value.ToString(format);

    public static implicit operator DateTime(ScoreCastDateTime scoreCastDate) => scoreCastDate.Value;
}
