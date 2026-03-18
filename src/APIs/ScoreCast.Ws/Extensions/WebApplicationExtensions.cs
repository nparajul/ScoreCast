using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using ScoreCast.Models.V1.Responses;

namespace ScoreCast.Ws.Extensions;

public static class WebApplicationExtensions
{
    public static void ConfigureScoreCastMiddlewares(this WebApplication app)
    {
        app.UseSerilogRequestLogging();
        app.UseCors("ScoreCastCorsPolicy");
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseFastEndpoints(c =>
        {
            c.Versioning.Prefix = "v";
            c.Versioning.DefaultVersion = 1;
            c.Versioning.PrependToRoute = true;
            c.Endpoints.RoutePrefix = "api";
            c.Errors.UseProblemDetails();
            c.Binding.UsePropertyNamingPolicy = true;
            c.Endpoints.ShortNames = true;
            c.Endpoints.Configurator = ep =>
            {
                ep.PreProcessor<FirebaseUserPreprocessor>(Order.Before);
            };
            c.Binding.UseDefaultValuesForNullableProps = false;
        }).UseSwaggerGen(opt => { }, uiConfig =>
        {
            if (!app.Environment.IsProduction() && !app.Environment.IsStaging()) return;
            uiConfig.DocExpansion = "none";
            uiConfig.DefaultModelsExpandDepth = -1;
            uiConfig.CustomHeadContent =
                "<style>.try-out, .execute-wrapper { display: none !important; }</style>";
        });

        app.UseExceptionHandler(a =>
        {
            var logger = a.ApplicationServices.GetRequiredService<ILogger<Program>>();
            var environment = a.ApplicationServices.GetRequiredService<IWebHostEnvironment>();

            a.Run(async context =>
            {
                var feature = context.Features.Get<IExceptionHandlerPathFeature>();
                var exception = feature!.Error;
                context.Response.StatusCode = 500;

                var errorMessage =
                    $"{feature.Path}:{exception.GetBaseException().Message}:{exception.Message}";

                if (Debugger.IsAttached)
                    Console.WriteLine(errorMessage);

                logger.LogError(exception,
                    "{Path} {BaseMessage}:{Message} :: {StackTrace}",
                    feature.Path, exception.GetBaseException().Message,
                    exception.Message, exception.StackTrace);

                var response = ScoreCastResponse.Exception(
                    environment.IsProduction() || environment.IsStaging()
                        ? "An unexpected error occurred."
                        : exception.Message);

                await context.Response.WriteAsJsonAsync(response);
            });
        });
    }
}
