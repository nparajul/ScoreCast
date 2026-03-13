using ScoreCast.Web.Components.Helpers;

namespace ScoreCast.Web.Extensions;

public static class ComponentServiceExtensions
{
    public static IServiceCollection AddScoreCastComponentServices(this IServiceCollection services)
    {
        services.AddScoped<INotifyService, NotifyService>();
        services.AddScoped<ILoadingService, LoadingService>();
        services.AddScoped<IAlertService, AlertService>();
        return services;
    }
}
