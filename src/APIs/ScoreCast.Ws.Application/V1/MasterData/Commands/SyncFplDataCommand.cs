using FastEndpoints;
using ScoreCast.Models.V1.Requests.MasterData;
using ScoreCast.Models.V1.Responses;

namespace ScoreCast.Ws.Application.V1.MasterData.Commands;

public record SyncFplDataCommand(SyncCompetitionRequest Request) : ICommand<ScoreCastResponse>;
