using Microsoft.AspNetCore.Http;

namespace ScoreCast.Ws.Endpoints.V1.MasterData;

public sealed class MasterDataGroup : Group
{
    public MasterDataGroup()
    {
        Configure("master-data",
            ep => { ep.Description(x => x.WithTags("MasterData")); });
    }
}
