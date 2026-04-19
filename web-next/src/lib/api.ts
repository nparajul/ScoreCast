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
  getSeasons: (code: string) => get<import("@/lib/types").SeasonResult[]>(`/football/seasons?competitionCode=${code}`),
  getGameweekMatches: (seasonId: number, gw: number) => get<import("@/lib/types").GameweekMatchesResult>(`/football/seasons/${seasonId}/gameweeks/${gw}/matches`),
  getPointsTable: (seasonId: number) => get<import("@/lib/types").PointsTableResult>(`/football/seasons/${seasonId}/table`),
  getCompetitionZones: (code: string) => get<import("@/lib/types").CompetitionZoneResult[]>(`/football/competitions/${code}/zones`),

  // Predictions
  getUserSeasons: () => get<import("@/lib/types").UserSeasonResult[]>("/predictions/user-seasons"),
};
