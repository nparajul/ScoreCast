using Hangfire;
using Hangfire.PostgreSql;
using ScoreCast.Jobs;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddSerilog((_, config) =>
        config.ReadFrom.Configuration(builder.Configuration));

    builder.Services.AddScoreCastApiClient(builder.Configuration);

    builder.Services.AddTransient<SyncMatchesJob>();

    builder.Services.AddHangfire(config => config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(options =>
            options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("ScoreCastDb")!)));

    builder.Services.AddHangfireServer();

    var host = builder.Build();

    var recurringJobs = host.Services.GetRequiredService<IRecurringJobManager>();

    recurringJobs.AddOrUpdate<SyncMatchesJob>(
        nameof(SyncMatchesJob),
        job => job.ExecuteAsync(),
        "0 */6 * * *"); // Every 6 hours

    Log.Information("ScoreCast.Jobs started");

    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "ScoreCast.Jobs terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
