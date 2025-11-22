namespace ScoreCast.Ws.Extensions;

public static class WebApplicationBuilderExtensions
{
    public static void AddApiCommonServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddHttpLogging(options => { options.CombineLogs = true; });

        var allowedOrigins = builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>();

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("ScoreCastCorsPolicy", policy =>
            {
                if (allowedOrigins != null)
                    policy.WithOrigins(allowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
            });
        });

        builder.Services.Configure<JsonOptions>(options =>
        {
            options.SerializerOptions.PropertyNameCaseInsensitive = true;
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.SerializerOptions.WriteIndented = true;
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            options.SerializerOptions.Encoder = JavaScriptEncoder.Default;
            options.SerializerOptions.AllowTrailingCommas = true;
            options.SerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString;
            options.SerializerOptions.UnknownTypeHandling = JsonUnknownTypeHandling.JsonElement;
            options.SerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip;
        });
    }
}
