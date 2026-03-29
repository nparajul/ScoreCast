using Microsoft.AspNetCore.Http;

namespace ScoreCast.Ws.Endpoints.V1.Share;

public sealed class ShareGroup : Group
{
    public ShareGroup()
    {
        Configure("share", ep =>
        {
            ep.AllowAnonymous();
            ep.Description(x => x.WithTags("Share"));
        });
    }
}
