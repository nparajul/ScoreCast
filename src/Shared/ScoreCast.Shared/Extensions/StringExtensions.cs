using System.Globalization;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ScoreCast.Shared.Constants;
using ScoreCast.Shared.Helpers.ObjectHelper;
using ScoreCast.Shared.Types;
using Microsoft.AspNetCore.Http;

namespace ScoreCast.Shared.Extensions;

public static partial class StringExtensions
{
    public static bool IsJsonContent(this string? inputString)
    {
        if (string.IsNullOrWhiteSpace(inputString)) return false;

        try
        {
            using var result = JsonDocument.Parse(inputString);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static string Left(this string? input, int length)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var inputLength = input.Length;

        return input[..(inputLength > length ? length : inputLength)];
    }

    public static int ToIntOrDefault(this string? inputString)
    {
        if (string.IsNullOrWhiteSpace(inputString))
            return 0;
        if (!int.TryParse(inputString, out var intValue)) intValue = 0;

        return intValue;
    }

    public static string? LeftOrNull(this string? input, int length)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var inputLength = input.Length;

        return input[..(inputLength > length ? length : inputLength)];
    }

    public static string LeftPadEmpty(this string? input, int length)
    {
        if (string.IsNullOrWhiteSpace(input))
            input = string.Empty;

        input = input.PadLeft(length, ' ');

        var inputLength = input.Length;

        return input[..(inputLength > length ? length : inputLength)];
    }

    public static string RightPaddEmpty(this string? input, int length)
    {
        if (string.IsNullOrWhiteSpace(input))
            input = string.Empty;

        input = input.PadRight(length, ' ');

        var inputLength = input.Length;

        return input[..(inputLength > length ? length : inputLength)];
    }


    public static string Right(this string? input, int length)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var inputLength = input.Length;

        return inputLength < length ? input : input[(inputLength - length)..];
    }

    public static string? ToTrimOrNull(this string? inputString)
    {
        return string.IsNullOrWhiteSpace(inputString) ? null : inputString.Trim();
    }

    public static string TrimUpperString(this string? inputString)
    {
        ArgumentException.ThrowIfNullOrEmpty(inputString);

        return inputString.Trim().ToUpper();
    }

    public static string ToTrimOrEmpty(this string? input)
    {
        return string.IsNullOrWhiteSpace(input) ? string.Empty : input.Trim();
    }

    public static string ToBase64(this string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        var plainTextBytes = Encoding.UTF8.GetBytes(input);

        return Convert.ToBase64String(plainTextBytes);
    }

    public static bool IsBase64String(this string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return false;
        if (input.Length % 4 != 0) return false;

        var base64Regex = Base64Regex();

        return base64Regex.IsMatch(input);
    }

    public static string FromBase64(this string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        if (!input.IsBase64String()) return input;

        var buffer = new Span<byte>(new byte[input.Length]);

        return Convert.TryFromBase64String(input, buffer, out var base64Bytes)
            ? Encoding.UTF8.GetString(buffer[..base64Bytes])
            : string.Empty;
    }

    public static string ToTrimUpper(this string? input)
    {
        return string.IsNullOrWhiteSpace(input) ? string.Empty : input.Trim().ToUpper();
    }

    public static string? TrimUpperOrNull(this string? input)
    {
        return string.IsNullOrWhiteSpace(input) ? null : input.Trim().ToUpper();
    }

    public static string ToTrimUpperRequired(this string? input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        return input.Trim().ToUpper();
    }

    public static string ToTrimRequired(this string? input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        return input.Trim();
    }

    public static string? ToNullOrTrimmed(this string? input)
    {
        return string.IsNullOrWhiteSpace(input) ? null : input.Trim();
    }

    public static long ToLongOrDefault(this string? inputString)
    {
        if (string.IsNullOrWhiteSpace(inputString))
            return 0;
        if (!long.TryParse(inputString, out var intValue)) intValue = 0;

        return intValue;
    }

    public static decimal ToDecimalOrDefault(this string? inputString)
    {
        if (string.IsNullOrWhiteSpace(inputString))
            return decimal.Zero;
        if (!decimal.TryParse(inputString, out var decimalValue)) decimalValue = decimal.Zero;

        return decimalValue;
    }

    public static string ToDecimalCeilString(this string? inputString)
    {
        if (string.IsNullOrWhiteSpace(inputString)) return "1";

        if (decimal.TryParse(inputString, out var decimalValue)) decimalValue = 1;

        return decimal.Ceiling(decimalValue).ToString(CultureInfo.InvariantCulture);
    }

    public static bool IsAbsoluteUrl(this string? inputString)
    {
        return !string.IsNullOrWhiteSpace(inputString) && Uri.TryCreate(inputString, UriKind.Absolute, out _);
    }

    public static bool IsEmail(this string? inputString)
    {
        return !string.IsNullOrWhiteSpace(inputString) && MailAddress.TryCreate(inputString, out _);
    }

    public static ScoreCastDateTime ToDateTimeOrNow(this string? inputString)
    {
        if (string.IsNullOrWhiteSpace(inputString))
            return ScoreCastDateTime.Now;

        return DateTime.TryParse(inputString, out var rtnDate) ? new ScoreCastDateTime(rtnDate) : ScoreCastDateTime.Now;
    }


    public static Stream AsStream(this string fileContent)
    {
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        writer.Write(fileContent);
        writer.Flush();
        stream.Position = 0;
        return stream;
    }

    public static T? FromJsonToObject<T>(this string? inputString)
    {
        return string.IsNullOrWhiteSpace(inputString)
            ? default
            : ObjectConverter.DeSerializeJson<T>(inputString);
    }

    public static T? FromXmlToObject<T>(this string? inputString)
    {
        return string.IsNullOrWhiteSpace(inputString)
            ? default
            : ObjectConverter.DeSerializeXml<T>(inputString);
    }

    public static bool? ToNullableBoolean(this string? inputString)
    {
        if (string.IsNullOrWhiteSpace(inputString)) return null;

        return inputString.ToLower() switch
        {
            "true" or "1" or "yes" or "y" or "x" => true,
            "false" or "0" or "no" or "n" => false,
            _ => throw new InvalidOperationException($"Invalid value: {inputString}")
        };
    }

    public static bool ToBool(this string? inputString)
    {
        return inputString.ToNullableBoolean() ?? false;
    }

    public static string ToDefaultValueIfNullOrEmpty(this string? value, string defaultValue)
    {
        return string.IsNullOrEmpty(value) ? defaultValue : value;
    }

    [GeneratedRegex(@"^[a-zA-Z0-9+/]*={0,2}$", RegexOptions.None)]
    private static partial Regex Base64Regex();

    public static string LastSegment(this PathString path)
    {
        if (!path.HasValue || string.IsNullOrEmpty(path.Value))
            return string.Empty;

        var segments = path.Value.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length > 0 ? segments[^1] : string.Empty;
    }

    public static string FirstSegment(this PathString path)
    {
        if (!path.HasValue || string.IsNullOrEmpty(path.Value))
            return string.Empty;

        var segments = path.Value.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length > 0 ? segments[0] : string.Empty;
    }

    public static string EndpointName(this PathString path)
    {
        if (!path.HasValue || string.IsNullOrEmpty(path.Value))
            return string.Empty;

        var segments = path.Value.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length > 2 ? segments[2] : segments[0];
    }

    public static string ToTrueFalseString(this string booleanStringValue)
    {
        var nullableBoolean = booleanStringValue.ToNullableBoolean();
        var booleanValue = nullableBoolean ?? false;

        return booleanValue.ConvertToTrueFalseString();
    }

    public static string[] SplitString(this string s, int chunkSize)
    {
        if (string.IsNullOrEmpty(s) || chunkSize <= 0) return [];

        return Enumerable.Range(0, (s.Length + chunkSize - 1) / chunkSize)
            .Select(i => s.Substring(i * chunkSize, Math.Min(s.Length - i * chunkSize, chunkSize)))
            .ToArray();
    }

    public static bool IsNotNullOrEmpty(this string? str)
    {
        return !string.IsNullOrEmpty(str);
    }

    public static bool IsNullOrEmpty(this string? str)
    {
        return string.IsNullOrWhiteSpace(str);
    }

    public static string OrAny(this string? str)
    {
        return str ?? SharedConstants.Any;
    }

}
