import { auth } from "@/lib/firebase";
import type { ScoreCastResponse } from "@/lib/types";

const BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL;

async function request<T>(path: string, options?: RequestInit): Promise<ScoreCastResponse<T>> {
  const token = auth.currentUser ? await auth.currentUser.getIdToken() : null;
  const method = options?.method ?? "GET";
  const hasBody = options?.body != null;
  const headers: Record<string, string> = {};
  if (hasBody) headers["Content-Type"] = "application/json";
  if (token) headers["Authorization"] = `Bearer ${token}`;

  try {
    const res = await fetch(`${BASE_URL}/api/v1${path}`, { ...options, method, headers: { ...headers, ...options?.headers } });
    const text = await res.text();
    if (!text) {
      return { resultType: res.ok ? "Ok" : "Error", success: res.ok, referenceId: "", message: res.ok ? undefined : `HTTP ${res.status}` } as ScoreCastResponse<T>;
    }
    try {
      return JSON.parse(text) as ScoreCastResponse<T>;
    } catch {
      return { resultType: "Error", success: false, referenceId: "", message: text } as ScoreCastResponse<T>;
    }
  } catch (err) {
    const msg = err instanceof Error ? err.message : "Network error";
    return { resultType: "Error", success: false, referenceId: "", message: msg } as ScoreCastResponse<T>;
  }
}

function get<T>(path: string) {
  return request<T>(path);
}

function post<T>(path: string, body?: unknown) {
  return request<T>(path, { method: "POST", body: body ? JSON.stringify(body) : undefined });
}

function put<T>(path: string, body?: unknown) {
  return request<T>(path, { method: "PUT", body: body ? JSON.stringify(body) : undefined });
}

export const api = {
  // Users
  syncUser: (appName: string) => post<import("@/lib/types").SyncUserResult>("/users/sync", { appName }),
  getMyProfile: () => get<import("@/lib/types").UserProfileResult>("/users/me"),
  updateMyProfile: (body: { displayName?: string; favouriteTeamId?: number; favoriteTeam?: string; hasCompletedOnboarding?: boolean }) => put<import("@/lib/types").UserProfileResult>("/users/me", body),
  resendVerificationEmail: () => post<void>("/users/me/resend-verification"),
  setUsername: (body: { username: string }) => put<import("@/lib/types").UserProfileResult>("/users/me/username", body),
  getMyRoles: () => get<import("@/lib/types").RoleResult[]>("/users/me/roles"),
  getRolePages: (roleId: number) => get<import("@/lib/types").PageResult[]>(`/users/roles/${roleId}/pages`),

  // Football
  getCompetitions: () => get<import("@/lib/types").CompetitionResult[]>("/football/competitions"),
  getDefaultCompetition: () => get<import("@/lib/types").CompetitionResult>("/football/competitions/default"),
  getSeasons: (code: string) => get<import("@/lib/types").SeasonResult[]>(`/football/competitions/${code}/seasons`),
  getCompetitionZones: (code: string) => get<import("@/lib/types").CompetitionZoneResult[]>(`/football/competitions/${code}/zones`),
  getGameweekMatches: (seasonId: number, gw: number) => get<import("@/lib/types").GameweekMatchesResult>(`/football/seasons/${seasonId}/gameweek/${gw}`),
  getPointsTable: (seasonId: number) => get<import("@/lib/types").PointsTableResult>(`/football/seasons/${seasonId}/table`),
  getBracket: (seasonId: number) => get<import("@/lib/types").BracketResult>(`/football/seasons/${seasonId}/bracket`),
  getPlayerStats: (seasonId: number) => get<import("@/lib/types").PlayerStatsResult>(`/football/seasons/${seasonId}/player-stats`),
  getMatchPage: (matchId: number) => get<import("@/lib/types").MatchPageResult>(`/football/matches/${matchId}`),
  getMatchExtras: (matchId: number) => get<import("@/lib/types").MatchExtrasResult>(`/football/matches/${matchId}/extras`),
  getMatchPrediction: (matchId: number) => get<import("@/lib/types").MatchPredictionResult>(`/football/matches/${matchId}/prediction`),
  getMatchHighlights: (matchId: number) => get<import("@/lib/types").MatchHighlightsResult>(`/football/matches/${matchId}/highlights`),
  getTeamDetail: (teamId: number) => get<import("@/lib/types").TeamDetailResult>(`/football/teams/${teamId}`),
  getTeamMatches: (teamId: number) => get<import("@/lib/types").TeamMatchesResult>(`/football/teams/${teamId}/matches`),
  getTeamSquad: (teamId: number) => get<import("@/lib/types").TeamSquadResult>(`/football/teams/${teamId}/squad`),
  getTeamPlayerStats: (teamId: number, seasonId?: number) => get<import("@/lib/types").PlayerStatsResult>(`/football/teams/${teamId}/player-stats${seasonId ? `?seasonId=${seasonId}` : ''}`),
  searchTeams: (q: string, skip = 0, take = 50) => get<import("@/lib/types").TeamSearchResult>(`/football/teams/search?q=${encodeURIComponent(q)}&skip=${skip}&take=${take}`),

  // Predictions
  getUserSeasons: () => get<import("@/lib/types").UserSeasonResult[]>("/prediction/user-seasons"),
  enrollUserSeason: (seasonId: number) => post<import("@/lib/types").UserSeasonResult>("/prediction/user-seasons", { seasonId }),
  leaveUserSeason: (seasonId: number) => request<void>(`/prediction/user-seasons/${seasonId}`, { method: "DELETE" }),
  reorderUserSeasons: (seasonIds: number[]) => put<void>("/prediction/user-seasons/reorder", { seasonIds }),
  getMyLeagues: () => get<import("@/lib/types").PredictionLeagueResult[]>("/prediction/leagues/mine"),
  getLeagueStandings: (leagueId: number) => get<import("@/lib/types").LeagueStandingsResult>(`/prediction/leagues/${leagueId}/standings`),
  createLeague: (body: { name: string; competitionId: number }) => post<import("@/lib/types").PredictionLeagueResult>("/prediction/leagues", body),
  joinLeague: (inviteCode: string) => post<import("@/lib/types").PredictionLeagueResult>("/prediction/leagues/join", { inviteCode }),
  getMyPredictionStats: () => get<import("@/lib/types").MyPredictionStatsResult>("/prediction/my-stats"),
  getMyPredictions: (seasonId: number, gameweekId: number) => get<import("@/lib/types").MyPredictionResult[]>(`/prediction/predictions/${seasonId}/${gameweekId}`),
  submitPredictions: (body: { seasonId: number; predictions: { matchId: number; predictedHomeScore: number; predictedAwayScore: number }[] }) => post<void>("/prediction/predictions", body),
  getMyRiskPlays: (seasonId: number, gameweekId: number) => get<import("@/lib/types").RiskPlayResult[]>(`/prediction/risk-plays/${seasonId}/${gameweekId}`),
  submitRiskPlays: (body: unknown) => post<void>("/prediction/risk-plays", body),
  getPlayerProfile: (userId: number, leagueId: number) => get<import("@/lib/types").PlayerProfileResult>(`/prediction/profile/${userId}/${leagueId}`),
  getPlayerGameweek: (userId: number, leagueId: number, seasonId: number, gameweekId: number) => get<import("@/lib/types").PlayerGameweekResult>(`/prediction/profile/${userId}/${leagueId}/${seasonId}/${gameweekId}`),
  getScoringRules: () => get<import("@/lib/types").ScoringRuleResult[]>("/prediction/scoring-rules"),
  calculateOutcomes: (body: { seasonId: number }) => post<void>("/prediction/calculate-outcomes", body),
  getPredictionReplay: (matchId: number, leagueId?: number) => get<import("@/lib/types").PredictionReplayResult>(`/prediction/replay/${matchId}${leagueId ? `/${leagueId}` : ""}`),

  // Community / Global
  getGlobalDashboard: (competition?: string) => get<import("@/lib/types").GlobalDashboardResult>(`/community/dashboard${competition ? `?competition=${competition}` : ""}`),
  getGlobalLeaderboard: (competition?: string) => get<import("@/lib/types").GlobalLeaderboardResult>(`/community/leaderboard${competition ? `?competition=${competition}` : ""}`),

  // Share (public — no auth required)
  getPublicPredictionReplay: (matchId: number, userId: number) => get<import("@/lib/types").PredictionReplayResult>(`/share/replay/${matchId}/${userId}/view`),
  getGameweekReplay: (seasonId: number, gw: number, userId: number) => get<import("@/lib/types").GameweekReplayResult>(`/share/gw-replay/${seasonId}/${gw}/${userId}`),

  // Insights
  getMatchInsights: (seasonId: number, gw: number) => get<import("@/lib/types").MatchInsightResult[]>(`/insights/upcoming?seasonId=${seasonId}&gameweekNumber=${gw}`),

  // Master data sync (admin)
  syncCompetition: (body: { competitionCode: string; appName: string }) => post<void>("/master-data/sync/competition", body),
  syncTeams: (body: { competitionCode: string; appName: string }) => post<void>("/master-data/sync/teams", body),
  syncMatches: (body: { competitionCode: string; syncAll?: boolean; appName: string }) => post<void>("/master-data/sync/matches", body),
  syncFplData: (body: { competitionCode: string; appName: string }) => post<void>("/master-data/sync/fpl", body),
  syncPulseEvents: (body: { competitionCode: string; batchSize: number; appName: string }) => post<{ total: number; processed: number; eventsAdded: number; complete: boolean }>("/master-data/sync/pulse-events", body),
  enhanceLiveMatches: (body?: { seasonId?: number }) => post<void>("/master-data/enhance-live", body ?? {}),
};
