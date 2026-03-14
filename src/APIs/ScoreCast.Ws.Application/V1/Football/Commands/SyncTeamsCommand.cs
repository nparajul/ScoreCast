using FastEndpoints;
using ScoreCast.Models.V1.Requests.Football;
using ScoreCast.Models.V1.Responses;

namespace ScoreCast.Ws.Application.V1.Football.Commands;

public record SyncTeamsCommand(SyncCompetitionRequest Request) : ICommand<ScoreCastResponse>;
