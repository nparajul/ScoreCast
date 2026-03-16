using System.ClientModel;
using Microsoft.Extensions.AI;
using OpenAI;

namespace ScoreCast.Ws.Extensions;

public static class AiExtensions
{
    public static void AddAiServices(this WebApplicationBuilder builder)
    {
        var token = builder.Configuration["AI:GitHubToken"];
        if (string.IsNullOrWhiteSpace(token)) return;

        var model = builder.Configuration["AI:Model"] ?? "gpt-4o-mini";

        var client = new OpenAIClient(new ApiKeyCredential(token),
            new OpenAIClientOptions { Endpoint = new Uri("https://models.inference.ai.azure.com") });

        builder.Services.AddSingleton<IChatClient>(client.GetChatClient(model).AsIChatClient());
    }
}
