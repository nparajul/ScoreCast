using ScoreCast.Models.V1.Responses;
using ScoreCast.Ws.Application.V1.Interfaces;

namespace ScoreCast.Ws.Application.V1.MasterData.Queries;

public record GetLiveSeasonsQuery : IQuery<ScoreCastResponse<List<long>>>;
