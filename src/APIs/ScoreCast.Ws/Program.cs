using System.Diagnostics;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    if (builder.Environment.IsEnvironment("Local"))
        builder.Configuration.AddUserSecrets<Program>();

    Log.Information("Starting {ApplicationName} API", builder.Environment.ApplicationName);

    builder.Host.UseSerilog((context, configuration) =>
        configuration.ReadFrom.Configuration(context.Configuration));

    builder.Services.AddFastEndpoints(opt =>
    {
        opt.Assemblies =
        [
            typeof(EndpointsGroup).Assembly,
            typeof(InfrastructureGroup).Assembly
        ];
    });

    builder.AddApiCommonServices();
    builder.AddScoreCastAuthentication();
    builder.AddScoreCastAuthorization();
    builder.AddScoreCastExternalHttpClients();
    builder.AddAiServices();
    builder.Services.AddScoreCastInfrastructure(builder.Environment.EnvironmentName);
    builder.Services.AddSingleton<IEmailService, SmtpEmailService>();
    builder.Services.AddHostedService<EnhanceLiveMatchesBackgroundService>();
    builder.Services.AddHostedService<CacheHighlightsBackgroundService>();
    builder.Services.AddHostedService<CleanupHighlightsBackgroundService>();

    builder.Services.AddApiVersioning(o =>
    {
        o.DefaultApiVersion = new ApiVersion(1, 0);
        o.AssumeDefaultVersionWhenUnspecified = true;
        o.ApiVersionReader = ApiVersionReader.Combine(
            new UrlSegmentApiVersionReader(),
            new QueryStringApiVersionReader("v"),
            new HeaderApiVersionReader("x-api-version"));
        o.ReportApiVersions = true;
        o.ApiVersionSelector = new CurrentImplementationApiVersionSelector(o);
    });

    builder.Services.SwaggerDocument(opt =>
    {
        opt.DocumentSettings = ds =>
        {
            ds.Title = "ScoreCast APIs";
            ds.Description = "ScoreCast Premier League Predictions API";
            ds.Version = "v1";
            ds.MarkNonNullablePropsAsRequired();
        };
        opt.ReleaseVersion = 1;
        opt.AutoTagPathSegmentIndex = 0;
        opt.EnableGetRequestsWithBody = false;
        opt.EnableJWTBearerAuth = !(builder.Environment.IsProduction() || builder.Environment.IsStaging());
        opt.ShortSchemaNames = true;
        opt.SerializerSettings = s =>
        {
            s.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            s.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            s.PropertyNameCaseInsensitive = true;
            s.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        };
    });

    var app = builder.Build();

    app.ConfigureScoreCastMiddlewares();

    await app.RunAsync();
}
catch (Exception ex)
{
    if (Debugger.IsAttached)
    {
        Console.WriteLine(ex);
        Debug.WriteLine(ex);
    }
    Log.Fatal(ex, "Application terminated unexpectedly - {Message}", ex.GetBaseException().Message);
}
finally
{
    await Log.CloseAndFlushAsync();
}
