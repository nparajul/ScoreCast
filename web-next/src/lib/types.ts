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
  homeTeamLogo?: string;
  awayTeamId: number;
  awayTeamName: string;
  awayTeamShortName?: string;
  awayTeamLogoUrl?: string;
  awayTeamLogo?: string;
  homeScore?: number;
  awayScore?: number;
  status: string;
  minute?: string;
  kickoffTime?: string;
  venue?: string;
  referee?: string;
  events: MatchEventDetail[];
}

export interface GameweekMatchesResult {
  gameweekId: number;
  gameweekNumber: number;
  totalGameweeks: number;
  currentGameweek: number;
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

export interface MatchEventDetail {
  playerName: string;
  eventType: string;
  value: number;
  isHome: boolean;
  minute?: string;
}

export interface MatchPageResult {
  matchId: number;
  kickoffTime?: string;
  status: string;
  minute?: string;
  firstHalfStartMillis?: number;
  phase?: string;
  secondHalfStartMillis?: number;
  homeTeamId: number;
  homeTeamName: string;
  homeTeamLogo?: string;
  homeTeamShortName: string;
  awayTeamId: number;
  awayTeamName: string;
  awayTeamLogo?: string;
  awayTeamShortName: string;
  homeScore?: number;
  awayScore?: number;
  venue?: string;
  referee?: string;
  halfTimeHomeScore?: number;
  halfTimeAwayScore?: number;
  competitionName: string;
  competitionLogo?: string;
  competitionCode: string;
  seasonId: number;
  homeFormation?: string;
  awayFormation?: string;
  homeCoach?: string;
  awayCoach?: string;
  homeCoachPhoto?: string;
  awayCoachPhoto?: string;
  homeLineup: MatchPageLineupPlayer[];
  homeSubs: MatchPageLineupPlayer[];
  awayLineup: MatchPageLineupPlayer[];
  awaySubs: MatchPageLineupPlayer[];
  events: MatchPageEvent[];
}

export interface MatchPageLineupPlayer {
  playerId: number;
  name: string;
  photoUrl?: string;
  shirtNumber?: number;
  position?: string;
  isCaptain: boolean;
  icons: string[];
  subMinute?: string;
}

export interface MatchPageEvent {
  eventType: string;
  playerName: string;
  assistName?: string;
  minute?: string;
  isHome: boolean;
  sortKey: number;
  playerOff?: string;
  runningScore?: string;
}

export interface MatchExtrasResult {
  headToHead: H2HMatch[];
  homeForm: FormEntry[];
  awayForm: FormEntry[];
  myPrediction?: UserMatchPrediction;
  community: CommunityPredictions;
  homePlayerStats: PlayerSeasonStat[];
  awayPlayerStats: PlayerSeasonStat[];
}

export interface H2HMatch {
  kickoffTime: string;
  matchId: number;
  homeTeam: string;
  awayTeam: string;
  homeLogo?: string;
  awayLogo?: string;
  homeScore: number;
  awayScore: number;
}

export interface FormEntry {
  kickoffTime: string;
  matchId: number;
  opponent: string;
  opponentLogo?: string;
  isHome: boolean;
  goalsFor: number;
  goalsAgainst: number;
  result: string;
}

export interface UserMatchPrediction {
  predictedHome: number;
  predictedAway: number;
  outcome?: string;
  points: number;
}

export interface CommunityPredictions {
  totalPredictions: number;
  homeWinPct: number;
  drawPct: number;
  awayWinPct: number;
  mostPopularScore?: string;
  mostPopularScorePct: number;
}

export interface PlayerSeasonStat {
  playerId: number;
  name: string;
  photoUrl?: string;
  position?: string;
  goals: number;
  assists: number;
  yellowCards: number;
  redCards: number;
}

export interface MatchHighlightsResult {
  videos: HighlightVideo[];
}

export interface HighlightVideo {
  title: string;
  embedHtml: string;
}


// ===== Bracket =====
export interface BracketResult {
  rounds: BracketRound[];
}

export interface BracketRound {
  name: string;
  slots: BracketSlot[];
}

export interface BracketSlot {
  home: string;
  away: string;
  date?: string;
  homeTeam?: string;
  awayTeam?: string;
  homeScore?: number;
  awayScore?: number;
}

// ===== Player stats =====
export interface PlayerStatsResult {
  rows: PlayerStatRow[];
}

export interface PlayerStatRow {
  playerId: number;
  playerName: string;
  playerImageUrl?: string;
  position?: string;
  teamId: number;
  teamName: string;
  teamLogo?: string;
  goals: number;
  penaltyGoals: number;
  assists: number;
  yellowCards: number;
  redCards: number;
  cleanSheets?: number;
}

// ===== Teams =====
export interface TeamResult {
  id: number;
  name: string;
  shortName?: string;
  logoUrl?: string;
  country?: string;
  venue?: string;
}

export interface TeamSearchResult {
  teams: TeamResult[];
  total: number;
  hasMore?: boolean;
  [key: string]: unknown;
}

export interface TeamDetailResult {
  id: number;
  name: string;
  shortName?: string;
  logoUrl?: string;
  countryName?: string;
  countryFlagUrl?: string;
  venue?: string;
  coach?: string;
  founded?: number;
  clubColors?: string;
  website?: string;
  nextMatch?: TeamNextMatch;
  form?: TeamFormMatch[];
  competitions?: TeamCompetitionSeason[];
}

export interface TeamNextMatch {
  matchId: number;
  opponentName: string;
  opponentLogoUrl?: string;
  kickoffTime?: string;
  isHome: boolean;
}

export interface TeamFormMatch {
  matchId: number;
  result: string;
  score?: string;
  opponent?: string;
}

export interface TeamCompetitionSeason {
  competitionId: number;
  competitionName: string;
  seasonId: number;
  seasonName: string;
}

export interface TeamMatchesResult {
  results?: TeamMatchDetail[];
  fixtures?: TeamMatchDetail[];
  matches?: TeamMatchDetail[];
  [key: string]: unknown;
}

export interface TeamMatchDetail {
  matchId: number;
  homeTeamId: number;
  homeTeamName: string;
  homeTeamShortName?: string;
  homeTeamLogo?: string;
  homeTeamLogoUrl?: string;
  awayTeamId: number;
  awayTeamName: string;
  awayTeamShortName?: string;
  awayTeamLogo?: string;
  awayTeamLogoUrl?: string;
  homeScore?: number;
  awayScore?: number;
  status: string;
  kickoffTime?: string;
  competition?: string;
  [key: string]: unknown;
}

export interface TeamSquadResult {
  players: SquadPlayer[];
}

export interface SquadPlayer {
  id?: number;
  playerId?: number;
  name: string;
  position?: string;
  shirtNumber?: number;
  nationality?: string;
  imageUrl?: string;
  photoUrl?: string;
  dateOfBirth?: string;
  [key: string]: unknown;
}

// ===== Predictions =====
export interface PredictionLeagueResult {
  id: number;
  name: string;
  inviteCode?: string;
  memberCount: number;
  competitionId?: number;
  competitionName?: string;
  competitionLogoUrl?: string;
  rank?: number;
  totalPoints?: number;
}

export interface MyPredictionStatsResult {
  totalPoints: number;
  exactScores: number;
  correctResults: number;
  predictionCount: number;
  totalPredictions?: number;
  currentStreak?: number;
  achievements?: string[];
  lastGameweek?: GameweekComparison;
  currentGameweek?: GameweekComparison;
  streak?: number;
  bestGameweek?: number;
  bestGameweekPoints?: number;
  avgPerGw?: number;
}

export interface GameweekComparison {
  gameweekNumber: number;
  yourPoints?: number;
  avgPoints?: number;
  topPoints?: number;
  userCorrect?: number;
  userTotal?: number;
  beatPct?: number;
  communityAvgCorrect?: number;
  communityAvgTotal?: number;
}

export interface MyPredictionResult {
  matchId: number;
  predictedHomeScore: number;
  predictedAwayScore: number;
  outcome?: string;
  pointsAwarded?: number;
}

export interface LeagueStandingsResult {
  leagueName: string;
  seasonId: number;
  startingGameweekNumber?: number;
  competitionName: string;
  competitionLogoUrl?: string;
  standings: LeagueStandingRow[];
}

export interface LeagueStandingRow {
  userId: number;
  displayName: string;
  avatarUrl?: string;
  totalPoints: number;
  exactScores: number;
  correctResults: number;
  predictionCount: number;
  gameweekPoints?: number;
  gameweekNumber?: number;
}

export interface PlayerProfileResult {
  id: number;
  userId: string;
  displayName: string;
  avatarUrl?: string;
  totalPoints: number;
  rank?: number;
  exactScores: number;
  correctResults: number;
  predictionCount: number;
  bestGameweek?: number;
  bestGameweekPoints?: number;
  avgPerGw?: number;
  averagePointsPerGameweek?: number;
  matchweeksPlayed?: number;
  favoriteTeam?: string;
  memberSince?: string;
}

export interface PlayerGameweekResult {
  predictionsVisible: boolean;
  riskPlaysVisible: boolean;
  predictions: MyPredictionResult[];
  riskPlays: RiskPlayResult[];
}

export interface RiskPlayResult {
  riskType: string;
  matchId: number;
  selection: string;
  bonusPoints: number;
  isWon: boolean;
  isResolved: boolean;
}

// ===== Global / Community =====
export interface GameweekCountdown {
  gameweekNumber: number;
  deadline?: string;
  totalPredictions: number;
  totalUsers: number;
  isComplete: boolean;
}

export interface MatchPredictionSummary {
  matchId: number;
  homeTeam: string;
  homeTeamShortName?: string;
  homeTeamLogo?: string;
  homeTeamCrest?: string;
  awayTeam: string;
  awayTeamShortName?: string;
  awayTeamLogo?: string;
  awayTeamCrest?: string;
  kickoffTime?: string;
  status: string;
  predictionCount: number;
  homeWinPercent?: number;
  drawPercent?: number;
  awayWinPercent?: number;
  homePct?: number;
  drawPct?: number;
  awayPct?: number;
  mostPredictedScore?: string;
  mostPredictedPct?: number;
  [key: string]: unknown;
}

export interface GlobalLeaderboardEntry {
  rank: number;
  userId: number;
  displayName?: string;
  username?: string;
  avatarUrl?: string;
  totalPoints: number;
  exactScores: number;
  predictionCount?: number;
  totalPredictions?: number;
  [key: string]: unknown;
}

export interface CommunityStats {
  totalPredictors: number;
  totalPredictions: number;
  exactScoreRate: number;
  exactScorePct?: number;
  exactScores?: number;
  hardestMatch?: string;
  hardestMatchAccuracy?: number;
  mostPredictableTeam?: string;
  mostPredictableTeamPct?: number;
  [key: string]: unknown;
}

export interface GameweekRecap {
  gameweekNumber: number;
  bestPredictor?: string;
  bestPredictorPoints?: number;
  exactScores?: number;
  totalExactScores?: number;
  totalPredictors?: number;
  biggestUpset?: string;
  boldestCall?: string;
  boldestCorrectPrediction?: string;
  [key: string]: unknown;
}

export interface GlobalDashboardResult {
  countdown?: GameweekCountdown;
  matches?: MatchPredictionSummary[];
  upcomingPredictions?: MatchPredictionSummary[];
  stats?: CommunityStats;
  community?: CommunityStats;
  recap?: GameweekRecap;
  lastGameweekRecap?: GameweekRecap;
  [key: string]: unknown;
}

export interface GlobalLeaderboardResult {
  entries: GlobalLeaderboardEntry[];
}

// ===== Replay =====
export interface ReplayGoalEvent {
  minute: string;
  team?: string;
  scorer?: string;
  isPredictionBreaker?: boolean;
  predictionStatus?: string;
  runningHome?: number;
  runningAway?: number;
  [key: string]: unknown;
}

export interface LeagueRivalComparison {
  userId: number;
  displayName: string;
  predictedHome: number;
  predictedAway: number;
  points: number;
  outcome?: string;
  [key: string]: unknown;
}

export interface ReplaySeasonAccuracy {
  totalPoints?: number;
  exactScores?: number;
  correctResults?: number;
  predictionCount?: number;
  totalPredictions?: number;
  accuracyPct?: number;
  trend?: string;
  [key: string]: unknown;
}

export interface PredictionReplayResult {
  matchId: number;
  homeTeam: string;
  homeTeamLogo?: string;
  homeLogo?: string;
  homeScore: number;
  awayTeam: string;
  awayTeamLogo?: string;
  awayLogo?: string;
  awayScore: number;
  kickoffTime?: string;
  status: string;
  playerName: string;
  displayName?: string;
  playerId: number;
  predictedHomeScore?: number;
  predictedAwayScore?: number;
  predictedHome?: number;
  predictedAway?: number;
  pointsAwarded?: number;
  pointsEarned?: number;
  outcome: string;
  deathMinute?: string;
  aiVerdict?: string;
  aiCommentary?: string;
  goals?: ReplayGoalEvent[];
  goalTimeline?: ReplayGoalEvent[];
  rivals?: LeagueRivalComparison[];
  leagueRivals?: LeagueRivalComparison[];
  seasonAccuracy?: ReplaySeasonAccuracy;
  [key: string]: unknown;
}

export interface GameweekReplayMatch {
  matchId: number;
  homeTeam: string;
  homeTeamLogo?: string;
  homeLogo?: string;
  homeScore: number;
  awayTeam: string;
  awayTeamLogo?: string;
  awayLogo?: string;
  awayScore: number;
  predictedHomeScore?: number;
  predictedAwayScore?: number;
  predictedHome?: number;
  predictedAway?: number;
  pointsAwarded?: number;
  points?: number;
  outcome: string;
  deathMinute?: string;
  [key: string]: unknown;
}

export interface GameweekReplayResult {
  gameweekNumber: number;
  playerName?: string;
  displayName?: string;
  competitionLogo?: string;
  matchesPredicted?: number;
  totalPoints: number;
  exactScores: number;
  correctResults: number;
  matches: GameweekReplayMatch[];
  [key: string]: unknown;
}

// ===== Insights =====
export interface MatchInsightResult {
  matchId: number;
  homeTeam: string;
  homeTeamName?: string;
  homeTeamShortName?: string;
  homeTeamLogo?: string;
  awayTeam: string;
  awayTeamName?: string;
  awayTeamShortName?: string;
  awayTeamLogo?: string;
  kickoffTime?: string;
  homeWinProbability?: number;
  homeWinPct?: number;
  drawProbability?: number;
  drawPct?: number;
  awayWinProbability?: number;
  awayWinPct?: number;
  homeXg?: number;
  awayXg?: number;
  aiSummary?: string;
  keyPlayers?: string[];
  topScoreline?: string;
  topScorelinePct?: number;
  [key: string]: unknown;
}
