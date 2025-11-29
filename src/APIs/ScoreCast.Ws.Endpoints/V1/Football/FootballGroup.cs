using Microsoft.AspNetCore.Http;

namespace ScoreCast.Ws.Endpoints.V1.Football;

public sealed class FootballGroup : Group
{
    public FootballGroup()
    {
        Configure("football",
            ep => { ep.Description(x => x.WithTags("Football")); });
    }
}
