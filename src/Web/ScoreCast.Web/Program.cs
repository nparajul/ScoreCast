using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using ScoreCast.Web;
using ScoreCast.Web.Auth;
using ScoreCast.Web.Extensions;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices();
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
