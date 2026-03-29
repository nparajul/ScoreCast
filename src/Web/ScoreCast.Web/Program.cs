using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using ScoreCast.Web;
using ScoreCast.Web.Auth;
using ScoreCast.Web.Extensions;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomCenter;
    config.SnackbarConfiguration.ShowTransitionDuration = 150;
    config.SnackbarConfiguration.HideTransitionDuration = 150;
    config.SnackbarConfiguration.VisibleStateDuration = 2000;
});
builder.Services.AddScoreCastComponentServices();
builder.AddScoreCastAuth();
builder.AddScoreCastApiClients();

var host = builder.Build();

var firebaseAuth = host.Services.GetRequiredService<ScoreCastAuthStateProvider>();
var config = host.Configuration.GetSection("Firebase").Value
             ?? System.Text.Json.JsonSerializer.Serialize(new
             {
                 apiKey = host.Configuration["Firebase:ApiKey"],
                 authDomain = host.Configuration["Firebase:AuthDomain"],
                 projectId = host.Configuration["Firebase:ProjectId"]
             });
await firebaseAuth.InitializeAsync(config);

await host.RunAsync();
