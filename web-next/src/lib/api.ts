import { auth } from "@/lib/firebase";
import type { ScoreCastResponse } from "@/lib/types";

const BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL;

async function request<T>(path: string, options?: RequestInit): Promise<ScoreCastResponse<T>> {
  const token = auth.currentUser ? await auth.currentUser.getIdToken() : null;
  const headers: Record<string, string> = { "Content-Type": "application/json" };
  if (token) headers["Authorization"] = `Bearer ${token}`;

  const res = await fetch(`${BASE_URL}/api/v1${path}`, { ...options, headers: { ...headers, ...options?.headers } });
  return res.json();
}

function get<T>(path: string) {
  return request<T>(path);
}

function post<T>(path: string, body?: unknown) {
  return request<T>(path, { method: "POST", body: body ? JSON.stringify(body) : undefined });
}

export const api = {
  // Auth / User
  syncUser: (appName: string) => post<import("@/lib/types").SyncUserResult>("/user/sync", { appName }),
  getMyProfile: () => get<import("@/lib/types").UserProfileResult>("/user/profile"),
  getRolePages: () => get<import("@/lib/types").PageResult[]>("/user/pages"),

  // Football
  getCompetitions: () => get<import("@/lib/types").CompetitionResult[]>("/football/competitions"),
  getDefaultCompetition: () => get<import("@/lib/types").CompetitionResult>("/football/competitions/default"),
  getSeasons: (code: string) => get<import("@/lib/types").SeasonResult[]>(`/football/competitions/${code}/seasons`),
  getGameweekMatches: (seasonId: number, gw: number) => get<import("@/lib/types").GameweekMatchesResult>(`/football/seasons/${seasonId}/gameweek/${gw}`),
  getPointsTable: (seasonId: number) => get<import("@/lib/types").PointsTableResult>(`/football/seasons/${seasonId}/table`),
  getCompetitionZones: (code: string) => get<import("@/lib/types").CompetitionZoneResult[]>(`/football/competitions/${code}/zones`),
  getMatchPage: (matchId: number) => get<import("@/lib/types").MatchPageResult>(`/football/matches/${matchId}`),
  getMatchExtras: (matchId: number) => get<import("@/lib/types").MatchExtrasResult>(`/football/matches/${matchId}/extras`),
  getMatchHighlights: (matchId: number) => get<import("@/lib/types").MatchHighlightsResult>(`/football/matches/${matchId}/highlights`),

  getBracket: (seasonId: number) => get<import("@/lib/types").BracketResult>(`/football/seasons/${seasonId}/bracket`),
  getPlayerStats: (seasonId: number) => get<import("@/lib/types").PlayerStatsResult>(`/football/seasons/${seasonId}/player-stats`),
  getTeamDetail: (teamId: number) => get<import("@/lib/types").TeamDetailResult>(`/football/teams/${teamId}`),
  getTeamMatches: (teamId: number, seasonId?: number) => get<import("@/lib/types").TeamMatchesResult>(`/football/teams/${teamId}/matches${seasonId ? `?seasonId=${seasonId}` : ""}`),
  getTeamSquad: (teamId: number, seasonId?: number) => get<import("@/lib/types").TeamSquadResult>(`/football/teams/${teamId}/squad${seasonId ? `?seasonId=${seasonId}` : ""}`),
  searchTeams: (q: string, skip = 0, take = 50) => get<import("@/lib/types").TeamSearchResult>(`/football/teams/search?q=${encodeURIComponent(q)}&skip=${skip}&take=${take}`),

  // Predictions
  getUserSeasons: () => get<import("@/lib/types").UserSeasonResult[]>("/predictions/user-seasons"),
  getMyLeagues: () => get<import("@/lib/types").PredictionLeagueResult[]>("/prediction/leagues/mine"),
  getMyPredictionStats: () => get<import("@/lib/types").MyPredictionStatsResult>("/prediction/my-stats"),
  getMyPredictions: (seasonId: number, gameweekId: number) => get<import("@/lib/types").MyPredictionResult[]>(`/prediction/predictions/${seasonId}/${gameweekId}`),
  submitPredictions: (body: { seasonId: number; predictions: { matchId: number; predictedHomeScore: number; predictedAwayScore: number }[] }) => post<void>("/prediction/predictions", body),
  getLeagueStandings: (leagueId: number) => get<import("@/lib/types").LeagueStandingsResult>(`/prediction/leagues/${leagueId}/standings`),
  getPlayerProfile: (userId: number, leagueId: number) => get<import("@/lib/types").PlayerProfileResult>(`/prediction/profile/${userId}/${leagueId}`),
  getPlayerGameweek: (userId: number, leagueId: number, seasonId: number, gameweekId: number) => get<import("@/lib/types").PlayerGameweekResult>(`/prediction/profile/${userId}/${leagueId}/${seasonId}/${gameweekId}`),
  reorderUserSeasons: (seasonIds: number[]) => request<void>("/predictions/user-seasons/reorder", { method: "PUT", body: JSON.stringify({ seasonIds }) }),
  enrollUserSeason: (seasonId: number) => post<import("@/lib/types").UserSeasonResult>("/prediction/user-seasons", { seasonId }),
  createLeague: (body: { name: string; competitionId: number }) => post<import("@/lib/types").PredictionLeagueResult>("/prediction/leagues", body),
  joinLeague: (inviteCode: string) => post<import("@/lib/types").PredictionLeagueResult>("/prediction/leagues/join", { inviteCode }),

  // Community / Global
  getGlobalDashboard: (competition?: string) => get<import("@/lib/types").GlobalDashboardResult>(`/community/dashboard${competition ? `?competition=${competition}` : ""}`),
  getGlobalLeaderboard: (competition?: string) => get<import("@/lib/types").GlobalLeaderboardResult>(`/community/leaderboard${competition ? `?competition=${competition}` : ""}`),

  // User profile
  updateMyProfile: (body: { displayName: string; favoriteTeam?: string }) =>
    request<import("@/lib/types").UserProfileResult>("/users/me", { method: "PUT", body: JSON.stringify(body) }),
  setUsername: (body: { username: string }) =>
    request<import("@/lib/types").UserProfileResult>("/users/me/username", { method: "PUT", body: JSON.stringify(body) }),

  // Replay (public)
  getPublicPredictionReplay: (matchId: number, userId: number) =>
    get<import("@/lib/types").PredictionReplayResult>(`/share/replay/${matchId}/${userId}/view`),
  getGameweekReplay: (seasonId: number, gw: number, userId: number) =>
    get<import("@/lib/types").GameweekReplayResult>(`/share/gw-replay/${seasonId}/${gw}/${userId}`),

  // Insights
  getMatchInsights: (seasonId: number, gw: number) =>
    get<import("@/lib/types").MatchInsightResult[]>(`/insights/upcoming?seasonId=${seasonId}&gameweekNumber=${gw}`),

  // Master data sync
  syncCompetition: (body: { competitionCode: string; appName: string }) =>
    post<void>("/master-data/sync/competition", body),
  syncTeams: (body: { competitionCode: string; appName: string }) =>
    post<void>("/master-data/sync/teams", body),
  syncMatches: (body: { competitionCode: string; syncAll?: boolean; appName: string }) =>
    post<void>("/master-data/sync/matches", body),
  syncFplData: (body: { competitionCode: string; appName: string }) =>
    post<void>("/master-data/sync/fpl", body),
  syncPulseEvents: (body: { competitionCode: string; batchSize: number; appName: string }) =>
    post<{ total: number; processed: number; eventsAdded: number; complete: boolean }>("/master-data/sync/pulse-events", body),
  enhanceLiveMatches: () => post<void>("/master-data/enhance-live", {}),
  calculateOutcomes: (body: { seasonId: number }) =>
    post<void>("/prediction/calculate-outcomes", body),
};
