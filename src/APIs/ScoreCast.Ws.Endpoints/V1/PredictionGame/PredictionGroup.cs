using Microsoft.AspNetCore.Http;

namespace ScoreCast.Ws.Endpoints.V1.PredictionGame;

public sealed class PredictionGroup : Group
{
    public PredictionGroup()
    {
        Configure("prediction",
            ep => { ep.Description(x => x.WithTags("Prediction")); });
    }
}
