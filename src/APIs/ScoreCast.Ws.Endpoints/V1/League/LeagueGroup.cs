using Microsoft.AspNetCore.Http;

namespace ScoreCast.Ws.Endpoints.V1.League;

public sealed class LeagueGroup : Group
{
    public LeagueGroup()
    {
        Configure("leagues",
            ep => { ep.Description(x => x.WithTags("Leagues")); });
    }
}
