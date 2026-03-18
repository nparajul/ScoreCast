using FastEndpoints;
using ScoreCast.Models.V1.Requests;
using ScoreCast.Models.V1.Responses;

namespace ScoreCast.Ws.Application.V1.Insights.Commands;

public record UpdateCurrentMatchdayCommand(ScoreCastRequest Request) : ICommand<ScoreCastResponse>;
