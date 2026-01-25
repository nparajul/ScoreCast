using Refit;
using ScoreCast.Models.V1.Requests.Prediction;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Prediction;

namespace ScoreCast.ApiClient.V1.Apis;

public partial interface IScoreCastApiClient
{
    [Post("/api/v1/prediction/leagues")]
    Task<ScoreCastResponse<PredictionLeagueResult>> CreatePredictionLeagueAsync([Body] CreatePredictionLeagueRequest request, CancellationToken ct);

    [Post("/api/v1/prediction/leagues/join")]
    Task<ScoreCastResponse<PredictionLeagueResult>> JoinPredictionLeagueAsync([Body] JoinPredictionLeagueRequest request, CancellationToken ct);

    [Get("/api/v1/prediction/leagues/mine")]
    Task<ScoreCastResponse<List<PredictionLeagueResult>>> GetMyLeaguesAsync(CancellationToken ct);

    [Post("/api/v1/prediction/predictions")]
    Task<ScoreCastResponse> SubmitPredictionsAsync([Body] SubmitPredictionsRequest request, CancellationToken ct);

    [Post("/api/v1/prediction/calculate-outcomes")]
    Task<ScoreCastResponse> CalculateOutcomesAsync([Body] CalculateOutcomesRequest request, CancellationToken ct);

    [Get("/api/v1/prediction/leagues/{predictionLeagueId}/standings")]
    Task<ScoreCastResponse<LeagueStandingsResult>> GetLeagueStandingsAsync(long predictionLeagueId, CancellationToken ct);

    [Get("/api/v1/prediction/predictions/{seasonId}/{gameweekId}")]
    Task<ScoreCastResponse<List<MyPredictionResult>>> GetMyPredictionsAsync(long seasonId, long gameweekId, CancellationToken ct);

    [Get("/api/v1/prediction/scoring-rules")]
    Task<ScoreCastResponse<List<ScoringRuleResult>>> GetScoringRulesAsync(CancellationToken ct);

    [Post("/api/v1/prediction/risk-plays")]
    Task<ScoreCastResponse> SubmitRiskPlaysAsync([Body] SubmitRiskPlaysRequest request, CancellationToken ct);

    [Get("/api/v1/prediction/risk-plays/{seasonId}/{gameweekId}")]
    Task<ScoreCastResponse<List<RiskPlayResult>>> GetMyRiskPlaysAsync(long seasonId, long gameweekId, CancellationToken ct);

    [Post("/api/v1/prediction/user-seasons")]
    Task<ScoreCastResponse<UserSeasonResult>> EnrollUserSeasonAsync([Body] EnrollUserSeasonRequest request, CancellationToken ct);

    [Get("/api/v1/prediction/user-seasons")]
    Task<ScoreCastResponse<List<UserSeasonResult>>> GetUserSeasonsAsync(CancellationToken ct);
}
