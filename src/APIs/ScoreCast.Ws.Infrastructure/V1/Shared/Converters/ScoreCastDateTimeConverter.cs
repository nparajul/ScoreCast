using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ScoreCast.Shared.Types;

namespace ScoreCast.Ws.Infrastructure.V1.Shared.Converters;

internal sealed class ScoreCastDateTimeConverter()
    : ValueConverter<ScoreCastDateTime, DateTime>(
        v => v.Value,
        v => new ScoreCastDateTime(v));
