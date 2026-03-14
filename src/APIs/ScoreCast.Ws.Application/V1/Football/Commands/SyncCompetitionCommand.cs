using FastEndpoints;
using ScoreCast.Models.V1.Requests.Football;
using ScoreCast.Models.V1.Responses;

namespace ScoreCast.Ws.Application.V1.Football.Commands;

public record SyncCompetitionCommand(SyncCompetitionRequest Request) : ICommand<ScoreCastResponse>;
