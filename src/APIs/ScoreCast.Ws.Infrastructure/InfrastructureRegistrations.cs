using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ScoreCast.Ws.Application.Interfaces;
using ScoreCast.Ws.Infrastructure.Data;

namespace ScoreCast.Ws.Infrastructure;

public static class InfrastructureRegistrations
{
    private const string ConnectionName = "ScoreCastDb";

    public static void AddScoreCastInfrastructure(this IServiceCollection services, string environmentName)
    {
        services.AddDbContext<IScoreCastDbContext, ScoreCastDbContext>((sp, opt) =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString(ConnectionName);

            ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

            opt.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly("ScoreCast.Ws.Migrations");
            });

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

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ScoreCastDbContext>());
    }
}
