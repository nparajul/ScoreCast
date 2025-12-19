using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ScoreCast.Shared.Constants;
using ScoreCast.Ws.Application;
using ScoreCast.Ws.Application.V1.Interfaces;
using ScoreCast.Ws.Infrastructure.Internal;
using ScoreCast.Ws.Infrastructure.V1.Shared;

namespace ScoreCast.Ws.Infrastructure;

public static class InfrastructureRegistrations
{
    private const string _connectionName = "ScoreCastDb";

    public static void AddScoreCastInfrastructure(this IServiceCollection services, string environmentName)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ScoreCastSaveChangesInterceptor>();

        services.AddDbContext<IScoreCastDbContext, ScoreCastDbContext>((sp, opt) =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString(_connectionName);
            ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

            opt.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.SetPostgresVersion(new Version(17,5));
                npgsql.MigrationsHistoryTable("sc_db_migrations_history", SharedConstants.DefaultSchema);
                npgsql.MigrationsAssembly("ScoreCast.Ws.Migrations");
            });

            using var scope = sp.CreateScope();
            var interceptor = sp.GetRequiredService<ScoreCastSaveChangesInterceptor>();
            opt.AddInterceptors(interceptor);

            if (environmentName.Equals("PRODUCTION", StringComparison.OrdinalIgnoreCase))
            {
                opt.EnableSensitiveDataLogging(false);
            }
            else
            {
                var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
                opt.UseLoggerFactory(loggerFactory);
                opt.EnableDetailedErrors();
                opt.EnableSensitiveDataLogging();
            }
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();
    }
}
