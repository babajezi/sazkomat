// API Response types matching backend DTOs

// ==================== AUTH TYPES ====================

export enum LanguagePreference {
  Czech = "Czech",
  English = "English"
}

export interface User {
  id: string;
  email: string;
  displayName: string | null;
  languagePreference: LanguagePreference;
  createdAt: string;
  isApproved: boolean;
  isAdmin: boolean;
}

export interface AuthResponse {
  token: string;
  expiresAt: string;
  user: User;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  displayName?: string;
  languagePreference?: LanguagePreference;
}

export interface GoogleLoginRequest {
  idToken: string;
  languagePreference?: LanguagePreference;
}

export interface UpdateLanguageRequest {
  languagePreference: LanguagePreference;
}

// Admin types
export interface UpdateUserRequest {
  displayName?: string;
  languagePreference?: LanguagePreference;
  isApproved?: boolean;
}

// ==================== OTHER TYPES ====================

// Enums
export enum ProviderType {
  Scraper = "Scraper",
  API = "API",
  Manual = "Manual",
  BettingProvider = "BettingProvider"
}

export enum MatchResult {
  Home = "H",
  Draw = "D",
  Away = "A"
}

export enum ImportJobStatus {
  Pending = "Pending",
  Running = "Running",
  Completed = "Completed",
  Failed = "Failed",
  PartialSuccess = "PartialSuccess"
}

export enum ImportJobType {
  Historical = "Historical",
  Incremental = "Incremental"
}

export enum SyncMode {
  Historical = "Historical",
  Current = "Current"
}

export enum SyncType {
  Countries = "Countries",
  Leagues = "Leagues",
  Seasons = "Seasons"
}

export enum MatchSortBy {
  Date = "date",
  Round = "round"
}

export enum BooleanFilterValue {
  All = "",
  True = "true",
  False = "false"
}

export enum HasProvidersFilter {
  All = "",
  Yes = "yes",
  No = "no"
}

export interface Sport {
  id: string;
  name: string;
  code: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface UpdateSportRequest {
  isActive?: boolean;
}

export interface Country {
  id: string;
  name: string;
  nameCs?: string | null;
  code: string;
  flagEmoji: string;
  isoCode: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  countryProviders?: CountryProvider[];
}

export interface CountryProvider {
  id: string;
  countryId: string;
  providerId: string;
  providerCode: string;
  providerName: string;
  isActive: boolean;
  metadata?: string | null;
  createdAt: string;
  updatedAt: string;
  provider?: DataProvider;
}

export interface League {
  id: string;
  sportId: string;
  countryId: string;
  name: string;
  nameCs?: string | null;
  displayName: string;
  betExplorerSlug: string;
  isBettable: boolean;
  isActive: boolean;
  priority: number;
  notes?: string | null;
  createdAt: string;
  updatedAt: string;
  sport?: Sport;
  country?: Country;
  leagueProviders?: LeagueProvider[];
}

export interface LeagueProvider {
  id: string;
  leagueId: string;
  providerId: string;
  providerSlug: string;
  providerName: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  provider?: DataProvider;
}

export interface CreateCountryRequest {
  name: string;
  code: string;
  flagEmoji: string;
}

export interface UpdateCountryRequest {
  name?: string;
  nameCs?: string;
  code?: string;
  flagEmoji?: string;
  isActive?: boolean;
}

export interface CreateLeagueRequest {
  sportId: string;
  countryId: string;
  name: string;
  displayName: string;
  betExplorerSlug: string;
  isBettable: boolean;
  isActive?: boolean;
  priority: number;
  notes?: string;
}

export interface UpdateLeagueRequest {
  name?: string;
  displayName?: string;
  betExplorerSlug?: string;
  isBettable?: boolean;
  isActive?: boolean;
  priority?: number;
  notes?: string;
}

export interface ToggleProviderSyncRequest {
  isActive: boolean;
}

export interface Season {
  id: string;
  name: string;
  startYear: number;
  endYear?: number | null;
  createdAt: string;
}

export interface LeagueSeason {
  id: string;
  leagueId: string;
  seasonId: string;
  seasonName: string;
  startYear: number;
  endYear?: number | null;
  isAvailableOnBetExplorer: boolean;
  hasData: boolean;
  hasOdds: boolean;
  roundsCount: number;
  matchesCount: number;
  lastScrapedAt?: string | null;
  syncEnabled: boolean;
  isCurrent: boolean;
  syncMode: SyncMode;
  lastDataSyncAt?: string | null;
}

export interface AvailableSeason extends Season {
  hasData: boolean;
  hasOdds: boolean;
  roundsCount: number;
  matchesCount: number;
  lastScrapedAt?: string | null;
}

export interface HistoricalImportRequest {
  leagueIds: string[];
  seasons?: string[];                   // Now optional - not required when importAllHistorical is true
  includeWithoutOdds: boolean;
  importAllHistorical?: boolean;        // Import all historical seasons (except current)
}

export interface ImportJob {
  id: string;
  leagueId: string;
  type: ImportJobType;
  status: ImportJobStatus;
  seasons: string[];
  includeWithoutOdds: boolean;
  startedAt: string;
  completedAt?: string | null;
  progress: ImportProgressData;
  createdAt: string;
  updatedAt: string;
  league?: League;
}

export interface ImportProgressData {
  totalSeasons: number;
  processedSeasons: string[]; // Array of completed season names
  totalRounds?: number;
  processedRounds: number;
  errors: string[]; // Array of error messages
  currentSeason?: string | null;
  currentRound?: number | null;
}

export interface ImportStats {
  leagueId: string;
  totalSeasons: number;
  totalRounds: number;
  earliestSeason: string;
  latestSeason: string;
  lastImportDate: string;
}

export interface DashboardStats {
  overall: OverallStats;
  results: MatchResultsStats;
  topLeagues: LeagueStats[];
  seasonBreakdown: SeasonStats[];
  recentJobs: RecentImportJob[];
}

export interface OverallStats {
  totalLeagues: number;
  totalRounds: number;
  totalSeasons: number;
  totalMatches: number;
}

export interface MatchResultsStats {
  homeWins: number;
  draws: number;
  awayWins: number;
  homeWinPercentage: number;
  drawPercentage: number;
  awayWinPercentage: number;
}

export interface LeagueStats {
  leagueId: string;
  leagueName: string;
  countryName: string;
  countryFlag: string;
  sportName: string;
  roundsCount: number;
  seasonsCount: number;
  matchesCount: number;
  lastImport: string | null;
}

export interface SeasonStats {
  season: string;
  roundsCount: number;
  matchesCount: number;
  leaguesCount: number;
}

export interface RecentImportJob {
  jobId: string;
  leagueId: string;
  leagueName: string;
  status: string;
  startedAt: string;
  completedAt: string | null;
  processedRounds: number;
  totalSeasons: number;
}

export interface ErrorResponse {
  error: string;
  errors?: string[];
  timestamp: string;
}

// Match types
export interface Match {
  id: string;
  homeTeam: string;
  awayTeam: string;
  homeScore: number;
  awayScore: number;
  result: MatchResult;
  homeOdds?: number | null;
  drawOdds?: number | null;
  awayOdds?: number | null;
  matchDate?: string | null;
  betExplorerUrl?: string | null;
  round: {
    id: string;
    season: string;
    roundNumber: number;
    leagueId: string;
  };
  league?: {
    id: string;
    name: string;
    displayName: string;
    country?: string | null;
    sport?: string | null;
  } | null;
}

export interface MatchFilter {
  leagueId?: string;
  season?: string;
  roundNumber?: number;
  result?: MatchResult;
  dateFrom?: string;
  dateTo?: string;
  teamName?: string;
  skip?: number;
  take?: number;
  sortBy?: MatchSortBy;
  sortDescending?: boolean;
}

export interface MatchesResponse {
  matches: Match[];
  totalCount: number;
  skip: number;
  take: number;
}

// Round types
export interface Round {
  id: string;
  leagueId: string;
  league?: {
    id: string;
    name: string;
    displayName: string;
    country: string;
    countryFlagEmoji: string;
    sport: string;
  };
  season: string;
  roundNumber: number;
  matchesCount: number;
  homeWins: number;
  draws: number;
  awayWins: number;
  summaryResult: string;
  cumulativeOddsHome: number;
  cumulativeOddsDraw: number;
  cumulativeOddsAway: number;
  oddsComplete: string;
  scrapedAt: string;
  dataSource: string;
  matches: RoundMatch[];
}

export interface RoundMatch {
  id: string;
  homeTeam: string;
  awayTeam: string;
  homeScore: number;
  awayScore: number;
  result: MatchResult;
  homeOdds?: number | null;
  drawOdds?: number | null;
  awayOdds?: number | null;
  matchDate?: string | null;
  betExplorerUrl?: string | null;
}

export interface RoundsResponse {
  rounds: Round[];
  totalCount: number;
  skip: number;
  take: number;
}

export interface RoundFilter {
  season?: string;
  leagueId?: string;
  skip?: number;
  take?: number;
  sortDescending?: boolean;
}

// Scan Capabilities
export interface ScanCapabilities {
  canScanCountries: boolean;
  canScanLeagues: boolean;
  canScanSeasons: boolean;
}

// Data Provider types
export interface DataProvider {
  id: string;
  name: string;
  code: string;
  baseUrl: string;
  isActive: boolean;
  priority: number;
  type: ProviderType;
  notes?: string | null;
  configuration?: string | null;
  scanCapabilities?: string | null;  // JSON string of ScanCapabilities
  hasLogo: boolean;
  logoUploadedAt?: string | null;
  createdAt: string;
  updatedAt: string;
}

// Helper function to parse scanCapabilities
export function parseScanCapabilities(scanCapabilitiesJson?: string | null): ScanCapabilities {
  const defaultCapabilities: ScanCapabilities = {
    canScanCountries: true,
    canScanLeagues: true,
    canScanSeasons: true
  };

  if (!scanCapabilitiesJson) {
    return defaultCapabilities;
  }

  try {
    return JSON.parse(scanCapabilitiesJson) as ScanCapabilities;
  } catch {
    return defaultCapabilities;
  }
}

export type LogoSize = 'sm' | 'md' | 'lg';

export interface SyncLeaguesRequest {
  sportCode: string;
}

// Sync types
export interface SyncRequest {
  providerId: string;
  type: SyncType;
  entityId?: string | null;
  activateCountries?: boolean;
}

export interface SyncResponse {
  success: boolean;
  message: string;
  statistics: SyncStatistics;
  completedAt: string;
  duration: string;
}

export interface SyncStatistics {
  totalProcessed: number;
  created: number;
  updated: number;
  skipped: number;
  errors: number;
  errorMessages: string[];
}

export interface SyncStatusResponse {
  isRunning: boolean;
  currentOperation?: string | null;
  startedAt?: string | null;
  lastCompletedAt?: string | null;
  lastResult?: SyncResponse | null;
}

export interface SyncWorkflowState {
  id: string;
  countriesSynced: boolean;
  countriesConfirmed: boolean;
  leaguesSynced: boolean;
  leaguesConfirmed: boolean;
  seasonsSynced: boolean;
  countriesSyncedAt?: string | null;
  leaguesSyncedAt?: string | null;
  seasonsSyncedAt?: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface UpdateSyncEnabledRequest {
  enabled: boolean;
}

export interface SyncSeasonDataRequest {
  providerId: string;
  forceUpdate?: boolean;
}

export interface AvailableSeasonsResponse {
  leagueId: string;
  leagueName: string;
  seasons: string[];
  currentSeason: string | null;
  historicalSeasons: string[];
}

// Country Provider CRUD requests
export interface CreateCountryProviderRequest {
  countryId: string;
  providerId: string;
  providerCode: string;
  providerName?: string;
  isActive?: boolean;
  metadata?: string;
}

export interface UpdateCountryProviderRequest {
  providerCode?: string;
  providerName?: string;
  isActive?: boolean;
  metadata?: string;
}

// League Provider CRUD requests
export interface CreateLeagueProviderRequest {
  leagueId: string;
  providerId: string;
  providerSlug: string;
  providerName?: string;
  isActive?: boolean;
}

export interface UpdateLeagueProviderRequest {
  providerSlug?: string;
  providerName?: string;
  isActive?: boolean;
}

// Scan & Job Queue types
export enum SyncJobType {
  Scan = "Scan",
  Import = "Import",
  LiveUpdate = "LiveUpdate"  // Backend uses LiveUpdate, not LiveSync
}

export enum SyncJobStatus {
  Pending = "Pending",
  Running = "Running",
  Completed = "Completed",
  PartiallyCompleted = "PartiallyCompleted",
  Failed = "Failed",
  Cancelled = "Cancelled"
}

export enum SyncEntityType {
  Countries = "Countries",
  Leagues = "Leagues",
  Seasons = "Seasons",
  Rounds = "Rounds",
  CountriesAndLeagues = "CountriesAndLeagues"
}

export enum MappingStatus {
  Unmapped = "Unmapped",
  AutoMapped = "AutoMapped",
  ManualMapped = "ManualMapped"
}

export interface ProviderCountry {
  id: string;
  providerId: string;
  providerCode: string;
  providerName: string;
  data: any;
  scannedAt: string;
  createdAt: string;
}

export interface ProviderLeague {
  id: string;
  providerId: string;
  providerSlug: string;
  providerName: string;
  displayName: string | null;
  countryCode: string;
  sportCode: string;
  mappingStatus: MappingStatus;
  data: any;
  scannedAt: string;
  createdAt: string;
}

export interface ProviderSeason {
  id: string;
  providerId: string;
  providerLeagueId: string;
  providerLeagueSlug: string;
  leagueName: string;
  leagueSlug: string;
  countryCode: string;
  countryName: string;
  countrySlug: string;
  seasonName: string;
  startYear: number;
  endYear: number | null;
  isCurrentSeason: boolean;
  data: any;
  scannedAt: string;
  createdAt: string;
  isImported: boolean;
  seasonId: string | null;
  importedAt: string | null;
}

export interface SyncJob {
  id: string;
  providerId: string;
  jobType: SyncJobType;
  entityType: SyncEntityType;
  status: SyncJobStatus;
  entityIds: string[] | null;
  startedAt: string | null;
  completedAt: string | null;
  errorMessage: string | null;
  createdAt: string;
  updatedAt: string;
  provider?: DataProvider;
}

export interface ScanCountriesRequest {
  providerId: string;
}

export interface ScanLeaguesRequest {
  providerId: string;
  countryIds: string[];
}

export interface ScanSeasonsRequest {
  providerId: string;
  leagueIds: string[];
}

export interface ScanJobResponse {
  jobId: string;
  message: string;
}

export interface LiveSyncRoundsRequest {
  providerId: string;
  leagueIds?: string[] | null;
  forceRefresh?: boolean;
}

export interface LiveSyncRoundRequest {
  providerId: string;
}

export interface LiveSyncStatsResponse {
  totalLeagues: number;
  totalRounds: number;
  lastSyncAt: string | null;
  roundsUpdatedToday: number;
}

// League Name Mappings
export interface LeagueNameMapping {
  id: string;
  providerCode: string;
  countryCode: string;
  providerLeagueName: string;
  betExplorerSlug: string;
  isActive: boolean;
  notes: string | null;
  priority: number;
  lastUsedAt: string | null;
  usageCount: number;
  lastProviderLeagueId: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface CreateLeagueNameMappingRequest {
  providerCode: string;
  countryCode: string;
  providerLeagueName: string;
  betExplorerSlug: string;
  isActive?: boolean;
  notes?: string;
  priority?: number;
}

export interface UpdateLeagueNameMappingRequest {
  providerLeagueName?: string;
  betExplorerSlug?: string;
  isActive?: boolean;
  notes?: string;
  priority?: number;
}

// Country Name Mappings
export interface CountryNameMapping {
  id: string;
  providerCode: string;
  providerCountryName: string;
  betExplorerCode: string;
  isActive: boolean;
  notes: string | null;
  priority: number;
  matchType: 'exact' | 'substring' | 'regex';
  isCaseSensitive: boolean;
  isSpecialCase: boolean;
  localizedName: string | null;
  lastUsedAt: string | null;
  usageCount: number;
  lastProviderCountryId: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface CreateCountryNameMappingRequest {
  providerCode: string;
  providerCountryName: string;
  betExplorerCode: string;
  isActive?: boolean;
  notes?: string;
  priority?: number;
  matchType?: 'exact' | 'substring' | 'regex';
  isCaseSensitive?: boolean;
  isSpecialCase?: boolean;
  localizedName?: string;
}

export interface UpdateCountryNameMappingRequest {
  providerCountryName?: string;
  betExplorerCode?: string;
  isActive?: boolean;
  notes?: string;
  priority?: number;
  matchType?: 'exact' | 'substring' | 'regex';
  isCaseSensitive?: boolean;
  isSpecialCase?: boolean;
  localizedName?: string;
}

// ==================== BETEXPLORER TYPES ====================

export interface BetExplorerLeague {
  name: string;
  slug: string;
  displayName: string;
  fromCache: boolean;
  cachedAt?: string;
}

// ==================== UNMATCHED LEAGUES ====================

export interface UnmatchedLeague {
  id: string;
  providerId: string;
  providerName?: string;
  providerLeagueId?: string;
  providerLeagueName: string;
  providerSlug?: string;
  countryCode: string;
  countryName?: string;
  scrapedAt: string;
  isResolved: boolean;
  resolutionType?: "Mapped" | "Ignored" | "Unavailable";
  resolvedLeagueId?: string;
  resolvedLeagueName?: string;
  resolvedAt?: string;
  resolutionNotes?: string;
}

export interface UnmatchedLeagueStats {
  total: number;
  unresolved: number;
  mapped: number;
  ignored: number;
  unavailable: number;
  byProvider: Array<{ provider: string; total: number; unresolved: number }>;
  topUnresolvedCountries: Array<{ country: string; count: number }>;
}
