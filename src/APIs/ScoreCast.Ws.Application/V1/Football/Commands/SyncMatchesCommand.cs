using FastEndpoints;
using ScoreCast.Models.V1.Requests.Football;
using ScoreCast.Models.V1.Responses;

namespace ScoreCast.Ws.Application.V1.Football.Commands;

public record SyncMatchesCommand(SyncCompetitionRequest Request) : ICommand<ScoreCastResponse>;
