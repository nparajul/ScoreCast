using Microsoft.AspNetCore.Http;
using ScoreCast.Ws.Endpoints.Preprocessors;

namespace ScoreCast.Ws.Endpoints.Extensions;

public static class HttpContextExtensions
{
    public static string GetFirebaseUserId(this HttpContext httpContext)
        => (string)httpContext.Items[FirebaseUserPreprocessor.FirebaseUserIdKey]!;
}
