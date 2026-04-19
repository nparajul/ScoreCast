export interface ScoreCastResponse<T = void> {
  resultType: "Ok" | "Error" | "NotFound" | "Exception";
  success: boolean;
  message?: string;
  code?: string;
  referenceId: string;
  data?: T;
}

export interface CompetitionResult {
  id: number;
  name: string;
  code: string;
  logoUrl?: string;
  countryName?: string;
  countryFlagUrl?: string;
}

export interface SeasonResult {
  id: number;
  name: string;
  isCurrent: boolean;
  startDate: string;
}

export interface UserProfileResult {
  id: number;
  userId: string;
  displayName?: string;
  email?: string;
  avatarUrl?: string;
  favouriteTeamId?: number;
  favouriteTeamName?: string;
  favouriteTeamLogoUrl?: string;
  hasCompletedOnboarding: boolean;
  memberSince: string;
}

export interface PageResult {
  route: string;
  displayName: string;
  icon?: string;
  displayOrder: number;
}

export interface UserSeasonResult {
  seasonId: number;
  competitionName: string;
  competitionCode: string;
  competitionLogoUrl?: string;
  seasonName: string;
  displayOrder: number;
}

export interface MatchDetail {
  matchId: number;
  homeTeamId: number;
  homeTeamName: string;
  homeTeamShortName?: string;
  homeTeamLogoUrl?: string;
  awayTeamId: number;
  awayTeamName: string;
  awayTeamShortName?: string;
  awayTeamLogoUrl?: string;
  homeScore?: number;
  awayScore?: number;
  status: string;
  minute?: string;
  kickoffTime?: string;
  venue?: string;
  referee?: string;
}

export interface GameweekMatchesResult {
  gameweekId: number;
  gameweekNumber: number;
  totalGameweeks: number;
  matches: MatchDetail[];
}

export interface PointsTableRow {
  position: number;
  teamId: number;
  teamName: string;
  teamShortName?: string;
  teamLogoUrl?: string;
  played: number;
  won: number;
  drawn: number;
  lost: number;
  goalsFor: number;
  goalsAgainst: number;
  goalDifference: number;
  points: number;
  form?: string;
}

export interface PointsTableGroup {
  groupName?: string;
  rows: PointsTableRow[];
}

export interface PointsTableResult {
  format: "League" | "GroupAndKnockout" | "Knockout";
  groups: PointsTableGroup[];
  bestThirdPlaced: PointsTableRow[];
  knockoutRounds: KnockoutRound[];
}

export interface KnockoutRound {
  name: string;
  sortOrder: number;
  matches: KnockoutMatch[];
}

export interface KnockoutMatch {
  matchId: number;
  homeTeam?: string;
  homeTeamLogo?: string;
  awayTeam?: string;
  awayTeamLogo?: string;
  homeScore?: number;
  awayScore?: number;
  status: string;
  kickoffTime?: string;
}

export interface CompetitionZoneResult {
  name: string;
  startPosition: number;
  endPosition: number;
  color: string;
}

export interface SyncUserResult {
  isNewUser: boolean;
}
