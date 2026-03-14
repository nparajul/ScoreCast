using FastEndpoints;
using ScoreCast.Models.V1.Requests.Football;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;

namespace ScoreCast.Ws.Application.V1.Football.Commands;

public record SyncPulseEventsCommand(SyncPulseEventsRequest Request) : ICommand<ScoreCastResponse<SyncPulseEventsResult>>;
