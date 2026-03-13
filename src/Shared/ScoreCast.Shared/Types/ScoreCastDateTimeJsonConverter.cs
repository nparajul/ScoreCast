using System.Text.Json;
using System.Text.Json.Serialization;

namespace ScoreCast.Shared.Types;

public sealed class ScoreCastDateTimeJsonConverter : JsonConverter<ScoreCastDateTime>
{
    public override ScoreCastDateTime? Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value is null ? null : new ScoreCastDateTime(value);
    }

    public override void Write(Utf8JsonWriter writer, ScoreCastDateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
