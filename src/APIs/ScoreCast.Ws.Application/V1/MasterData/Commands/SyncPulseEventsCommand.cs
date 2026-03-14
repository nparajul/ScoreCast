using FastEndpoints;
using ScoreCast.Models.V1.Requests.MasterData;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.MasterData;

namespace ScoreCast.Ws.Application.V1.MasterData.Commands;

public record SyncPulseEventsCommand(SyncPulseEventsRequest Request) : ICommand<ScoreCastResponse<SyncPulseEventsResult>>;
