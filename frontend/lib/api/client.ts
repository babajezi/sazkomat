import axios from "axios";
import type {
  Sport,
  UpdateSportRequest,
  Country,
  CountryProvider,
  League,
  LeagueProvider,
  CreateCountryRequest,
  UpdateCountryRequest,
  CreateCountryProviderRequest,
  UpdateCountryProviderRequest,
  CreateLeagueProviderRequest,
  UpdateLeagueProviderRequest,
  CreateLeagueRequest,
  UpdateLeagueRequest,
  ToggleProviderSyncRequest,
  HistoricalImportRequest,
  ImportJob,
  ImportStats,
  DashboardStats,
  RoundsResponse,
  RoundFilter,
  LeagueSeason,
  UpdateSyncEnabledRequest,
  SyncRequest,
  SyncResponse,
  SyncSeasonDataRequest,
  AvailableSeasonsResponse,
  DataProvider,
  LogoSize,
  SyncLeaguesRequest,
  LeagueNameMapping,
  CreateLeagueNameMappingRequest,
  UpdateLeagueNameMappingRequest,
  CountryNameMapping,
  CreateCountryNameMappingRequest,
  UpdateCountryNameMappingRequest,
  UnmatchedLeague,
  UnmatchedLeagueStats,
  UnmatchedCountry,
  UnmatchedCountryStats,
  BetExplorerLeague,
  CopyResolutionsPreviewResponse,
  CopyResolutionsExecuteResponse,
  GlobalRulePreview,
  GlobalRuleResult,
  // Auth types
  User,
  AuthResponse,
  LoginRequest,
  RegisterRequest,
  GoogleLoginRequest,
  UpdateLanguageRequest,
  UpdateUserRequest,
  // Recipe types
  RecipeListItem,
  ScraperRecipe,
  CreateRecipeRequest,
  UpdateRecipeRequest,
  TestRecipeRequest,
  TestRecipeResponse,
  RecipeStats,
  // Validation types
  LeagueValidationResult,
  UpdateIgnoredRequest,
  // Analytics types
  ViewSpec,
  AnalyticsResult,
  AnalyticsViewListItem,
  AnalyticsViewDetail,
  CreateViewRequest,
  UpdateViewRequest,
  AnalyticsMetadata,
} from "./types";

const API_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000";

const apiClient = axios.create({
  baseURL: `${API_URL}/api`,
  headers: {
    "Content-Type": "application/json",
  },
});

// Configuration endpoints
export const configApi = {
  getSports: async (): Promise<Sport[]> => {
    const { data } = await apiClient.get<Sport[]>("/config/sports");
    return data;
  },

  updateSport: async (
    id: string,
    request: UpdateSportRequest
  ): Promise<Sport> => {
    const { data } = await apiClient.patch<Sport>(
      `/config/sports/${id}`,
      request
    );
    return data;
  },

  getCountries: async (sportId?: string): Promise<Country[]> => {
    const { data } = await apiClient.get<Country[]>("/config/countries", {
      params: { sportId },
    });
    return data;
  },

  createCountry: async (request: CreateCountryRequest): Promise<Country> => {
    const { data } = await apiClient.post<Country>("/config/countries", request);
    return data;
  },

  updateCountry: async (
    id: string,
    request: UpdateCountryRequest
  ): Promise<Country> => {
    const { data } = await apiClient.patch<Country>(
      `/config/countries/${id}`,
      request
    );
    return data;
  },

  deleteCountry: async (id: string): Promise<void> => {
    await apiClient.delete(`/config/countries/${id}`);
  },

  toggleCountryProviderSync: async (
    countryId: string,
    providerId: string,
    request: ToggleProviderSyncRequest
  ): Promise<CountryProvider> => {
    const { data } = await apiClient.patch<CountryProvider>(
      `/config/countries/${countryId}/providers/${providerId}`,
      request
    );
    return data;
  },

  // Country provider CRUD operations
  createCountryProvider: async (
    request: CreateCountryProviderRequest
  ): Promise<CountryProvider> => {
    const { data } = await apiClient.post<CountryProvider>(
      "/config/providers/country-mappings",
      request
    );
    return data;
  },

  updateCountryProvider: async (
    id: string,
    request: UpdateCountryProviderRequest
  ): Promise<CountryProvider> => {
    const { data } = await apiClient.patch<CountryProvider>(
      `/config/providers/country-mappings/${id}`,
      request
    );
    return data;
  },

  deleteCountryProvider: async (id: string): Promise<void> => {
    await apiClient.delete(`/config/providers/country-mappings/${id}`);
  },

  // League provider CRUD operations
  createLeagueProvider: async (
    request: CreateLeagueProviderRequest
  ): Promise<LeagueProvider> => {
    const { data } = await apiClient.post<LeagueProvider>(
      "/config/providers/league-mappings",
      request
    );
    return data;
  },

  updateLeagueProvider: async (
    id: string,
    request: UpdateLeagueProviderRequest
  ): Promise<LeagueProvider> => {
    const { data } = await apiClient.patch<LeagueProvider>(
      `/config/providers/league-mappings/${id}`,
      request
    );
    return data;
  },

  deleteLeagueProvider: async (id: string): Promise<void> => {
    await apiClient.delete(`/config/providers/league-mappings/${id}`);
  },

  getLeagues: async (params?: {
    sportId?: string;
    countryId?: string;
    onlyEnabled?: boolean;
  }): Promise<League[]> => {
    const { data } = await apiClient.get<League[]>("/config/leagues", {
      params,
    });
    return data;
  },

  createLeague: async (request: CreateLeagueRequest): Promise<League> => {
    const { data } = await apiClient.post<League>("/config/leagues", request);
    return data;
  },

  updateLeague: async (
    id: string,
    request: UpdateLeagueRequest
  ): Promise<League> => {
    const { data } = await apiClient.patch<League>(
      `/config/leagues/${id}`,
      request
    );
    return data;
  },

  deleteLeague: async (id: string, ignoreInProvider: boolean = false): Promise<void> => {
    await apiClient.delete(`/config/leagues/${id}`, {
      params: { ignoreInProvider },
    });
  },

  toggleLeagueProviderSync: async (
    leagueId: string,
    providerId: string,
    request: ToggleProviderSyncRequest
  ): Promise<LeagueProvider> => {
    const { data} = await apiClient.patch<LeagueProvider>(
      `/config/leagues/${leagueId}/providers/${providerId}`,
      request
    );
    return data;
  },

  // Providers endpoints
  getProviders: async (): Promise<DataProvider[]> => {
    const { data } = await apiClient.get<DataProvider[]>("/config/providers");
    return data;
  },

  getBettingProviders: async (): Promise<DataProvider[]> => {
    const { data } = await apiClient.get<DataProvider[]>("/config/providers/betting");
    return data;
  },

  syncBettingProviderLeagues: async (
    providerId: string,
    request: SyncLeaguesRequest
  ): Promise<{ message: string }> => {
    const { data } = await apiClient.post(
      `/config/providers/${providerId}/sync-leagues`,
      request
    );
    return data;
  },

  autoEnableBetExplorerSync: async (): Promise<{ message: string }> => {
    const { data } = await apiClient.post("/config/providers/auto-enable-betexplorer");
    return data;
  },

  getLeagueBettingAvailability: async (leagueId: string): Promise<any[]> => {
    const { data } = await apiClient.get(`/config/leagues/${leagueId}/betting-availability`);
    return data;
  },

  getProviderSyncStatus: async (providerId: string, sportCode?: string): Promise<{ providerCode: string; sportCode?: string; status: string }> => {
    const { data } = await apiClient.get(`/config/providers/${providerId}/sync-status`, {
      params: { sportCode }
    });
    return data;
  },

  updateProviderCredentials: async (
    providerId: string,
    credentials: { username?: string; password?: string; sessionCookies?: string }
  ): Promise<DataProvider> => {
    const { data } = await apiClient.patch(`/config/providers/${providerId}/credentials`, credentials);
    return data;
  },

  updateProviderConfiguration: async (
    providerId: string,
    config: {
      timeout?: number;
      proxyUrl?: string;
      excludedCountryIds?: string[];
      excludedLeagueIds?: string[];
      customSettings?: Record<string, string>;
    }
  ): Promise<DataProvider> => {
    const { data } = await apiClient.patch(`/config/providers/${providerId}/configuration`, config);
    return data;
  },

  // Provider Logo endpoints
  uploadProviderLogo: async (providerId: string, file: File): Promise<{ message: string }> => {
    const formData = new FormData();
    formData.append("file", file);
    const { data } = await apiClient.post(`/config/providers/${providerId}/logo`, formData, {
      headers: {
        "Content-Type": "multipart/form-data",
      },
    });
    return data;
  },

  deleteProviderLogo: async (providerId: string): Promise<{ message: string }> => {
    const { data } = await apiClient.delete(`/config/providers/${providerId}/logo`);
    return data;
  },

  getProviderLogoUrl: (providerId: string, size: "sm" | "md" | "lg" = "md"): string => {
    return `${API_URL}/api/config/providers/${providerId}/logo?size=${size}`;
  },
};

// Import endpoints
export const importApi = {
  getAvailableLeagues: async (sportId?: string): Promise<League[]> => {
    const { data } = await apiClient.get<League[]>(
      "/import/leagues/available",
      {
        params: { sportId },
      }
    );
    return data;
  },

  startHistoricalImport: async (
    request: HistoricalImportRequest
  ): Promise<{
    jobId: string;
    message: string;
    job: ImportJob;
  }> => {
    const { data } = await apiClient.post("/import/historical", request);
    return data;
  },

  getJobStatus: async (jobId: string): Promise<ImportJob> => {
    const { data } = await apiClient.get<ImportJob>(`/import/jobs/${jobId}`);
    return data;
  },

  getImportStats: async (leagueId: string): Promise<ImportStats> => {
    const { data } = await apiClient.get<ImportStats>("/import/stats", {
      params: { leagueId },
    });
    return data;
  },

  getDashboardStats: async (): Promise<DashboardStats> => {
    const { data } = await apiClient.get<DashboardStats>("/import/dashboard");
    return data;
  },

  getRounds: async (params?: RoundFilter): Promise<RoundsResponse> => {
    const { data } = await apiClient.get<RoundsResponse>("/import/rounds", { params });
    return data;
  },

  getAvailableSeasons: async (
    leagueId: string
  ): Promise<AvailableSeasonsResponse> => {
    const { data } = await apiClient.get<AvailableSeasonsResponse>(
      `/import/leagues/${leagueId}/seasons/available`
    );
    return data;
  },
};

// Season endpoints
export const seasonApi = {
  getLeagueSeasons: async (leagueId?: string): Promise<LeagueSeason[]> => {
    const { data } = await apiClient.get<LeagueSeason[]>("/config/seasons/league-seasons", {
      params: { leagueId },
    });
    return data;
  },

  updateSyncEnabled: async (
    leagueSeasonId: string,
    request: UpdateSyncEnabledRequest
  ): Promise<void> => {
    await apiClient.patch(
      `/config/seasons/league-seasons/${leagueSeasonId}/sync-enabled`,
      request
    );
  },

  // League-level validation endpoints
  validateLeague: async (leagueId: string): Promise<LeagueValidationResult> => {
    const { data } = await apiClient.post<LeagueValidationResult>(
      `/config/leagues/${leagueId}/validate`
    );
    return data;
  },

  lockValidSeasons: async (leagueId: string): Promise<{ message: string; lockedCount: number }> => {
    const { data } = await apiClient.post<{ message: string; lockedCount: number }>(
      `/config/leagues/${leagueId}/lock`
    );
    return data;
  },

  unlockAllSeasons: async (leagueId: string): Promise<{ message: string; unlockedCount: number }> => {
    const { data } = await apiClient.post<{ message: string; unlockedCount: number }>(
      `/config/leagues/${leagueId}/unlock`
    );
    return data;
  },

  updateIgnoredStatus: async (
    leagueSeasonId: string,
    request: UpdateIgnoredRequest
  ): Promise<void> => {
    await apiClient.patch(
      `/config/seasons/league-seasons/${leagueSeasonId}/ignore`,
      request
    );
  },
};

// Sync endpoints
export const syncApi = {
  detectCurrentSeasons: async (request: SyncRequest): Promise<{ message: string }> => {
    const { data } = await apiClient.post("/sync/seasons/detect-current", request);
    return data;
  },

  syncAllMarkedSeasonsData: async (request: SyncRequest): Promise<SyncResponse> => {
    const { data } = await apiClient.post<SyncResponse>("/sync/seasons/data", request);
    return data;
  },

  syncSeasonData: async (
    leagueId: string,
    seasonId: string,
    request: SyncSeasonDataRequest
  ): Promise<SyncResponse> => {
    const { data } = await apiClient.post<SyncResponse>(
      `/sync/seasons/data/${leagueId}/${seasonId}`,
      request
    );
    return data;
  },

  // Sync rounds and matches for all seasons of a league
  syncLeagueSeasonData: async (
    leagueId: string,
    forceUpdate: boolean = false
  ): Promise<{ jobId: string; message: string }> => {
    const { data } = await apiClient.post<{ jobId: string; message: string }>(
      `/sync/league/${leagueId}/season-data`,
      { forceUpdate }
    );
    return data;
  },

  // Refresh list of available seasons from BetExplorer (metadata only)
  refreshLeagueSeasonsList: async (
    leagueId: string
  ): Promise<{ jobId: string; message: string }> => {
    const { data } = await apiClient.post<{ jobId: string; message: string }>(
      `/sync/league/${leagueId}/seasons-list`
    );
    return data;
  },
};

// League Name Mapping endpoints
export const mappingApi = {
  getMappings: async (params?: {
    providerCode?: string;
    countryCode?: string;
    isActive?: boolean;
  }): Promise<LeagueNameMapping[]> => {
    const { data } = await apiClient.get<LeagueNameMapping[]>("/mappings", {
      params,
    });
    return data;
  },

  getMappingById: async (id: string): Promise<LeagueNameMapping> => {
    const { data } = await apiClient.get<LeagueNameMapping>(`/mappings/${id}`);
    return data;
  },

  createMapping: async (
    request: CreateLeagueNameMappingRequest
  ): Promise<LeagueNameMapping> => {
    const { data } = await apiClient.post<LeagueNameMapping>(
      "/mappings",
      request
    );
    return data;
  },

  updateMapping: async (
    id: string,
    request: UpdateLeagueNameMappingRequest
  ): Promise<LeagueNameMapping> => {
    const { data } = await apiClient.patch<LeagueNameMapping>(
      `/mappings/${id}`,
      request
    );
    return data;
  },

  deleteMapping: async (id: string): Promise<void> => {
    await apiClient.delete(`/mappings/${id}`);
  },

  toggleMappingActive: async (id: string): Promise<LeagueNameMapping> => {
    const { data } = await apiClient.post<LeagueNameMapping>(
      `/mappings/${id}/toggle`
    );
    return data;
  },
};

export const countryMappingApi = {
  getMappings: async (params?: {
    providerCode?: string;
    isActive?: boolean;
  }): Promise<CountryNameMapping[]> => {
    const { data } = await apiClient.get<CountryNameMapping[]>(
      "/country-mappings",
      {
        params,
      }
    );
    return data;
  },

  getMappingById: async (id: string): Promise<CountryNameMapping> => {
    const { data } = await apiClient.get<CountryNameMapping>(
      `/country-mappings/${id}`
    );
    return data;
  },

  createMapping: async (
    request: CreateCountryNameMappingRequest
  ): Promise<CountryNameMapping> => {
    const { data } = await apiClient.post<CountryNameMapping>(
      "/country-mappings",
      request
    );
    return data;
  },

  updateMapping: async (
    id: string,
    request: UpdateCountryNameMappingRequest
  ): Promise<CountryNameMapping> => {
    const { data } = await apiClient.patch<CountryNameMapping>(
      `/country-mappings/${id}`,
      request
    );
    return data;
  },

  deleteMapping: async (id: string): Promise<void> => {
    await apiClient.delete(`/country-mappings/${id}`);
  },

  toggleMappingActive: async (id: string): Promise<CountryNameMapping> => {
    const { data } = await apiClient.post<CountryNameMapping>(
      `/country-mappings/${id}/toggle`
    );
    return data;
  },

  applyMappings: async (providerId: string): Promise<{ createdCount: number; message: string }> => {
    const { data } = await apiClient.post<{ createdCount: number; message: string }>(
      `/scan/apply-country-mappings`,
      { providerId }
    );
    return data;
  },
};

// Auth endpoints
const TOKEN_KEY = "sazkomat_token";

export const authApi = {
  // Get stored token
  getToken: (): string | null => {
    if (typeof window === "undefined") return null;
    return localStorage.getItem(TOKEN_KEY);
  },

  // Store token (localStorage + cookie for middleware)
  setToken: (token: string): void => {
    if (typeof window !== "undefined") {
      localStorage.setItem(TOKEN_KEY, token);
      // Also set cookie for Next.js middleware
      document.cookie = `sazkomat_token=${token}; path=/; max-age=${60 * 60 * 24 * 7}; SameSite=Lax`;
    }
  },

  // Remove token (from both localStorage and cookie)
  removeToken: (): void => {
    if (typeof window !== "undefined") {
      localStorage.removeItem(TOKEN_KEY);
      // Also remove cookie
      document.cookie = 'sazkomat_token=; path=/; max-age=0';
    }
  },

  // Register new user
  register: async (request: RegisterRequest): Promise<AuthResponse> => {
    const { data } = await apiClient.post<AuthResponse>("/auth/register", request);
    // Only set token if user is approved (token will be empty string if not)
    if (data.token) {
      authApi.setToken(data.token);
    }
    return data;
  },

  // Login with email/password
  login: async (request: LoginRequest): Promise<AuthResponse> => {
    const { data } = await apiClient.post<AuthResponse>("/auth/login", request);
    // Only set token if user is approved
    if (data.token) {
      authApi.setToken(data.token);
    }
    return data;
  },

  // Login with Google ID token
  googleLogin: async (request: GoogleLoginRequest): Promise<AuthResponse> => {
    const { data } = await apiClient.post<AuthResponse>("/auth/google", request);
    // Only set token if user is approved
    if (data.token) {
      authApi.setToken(data.token);
    }
    return data;
  },

  // Get current user
  getMe: async (): Promise<User> => {
    const { data } = await apiClient.get<User>("/auth/me");
    return data;
  },

  // Update language preference
  updateLanguage: async (request: UpdateLanguageRequest): Promise<User> => {
    const { data } = await apiClient.patch<User>("/auth/me/language", request);
    return data;
  },

  // Logout
  logout: async (): Promise<void> => {
    try {
      await apiClient.post("/auth/logout");
    } finally {
      authApi.removeToken();
    }
  },
};

// BetExplorer API - for fetching leagues from BetExplorer
export const betExplorerApi = {
  // Get leagues from BetExplorer for a country (with caching)
  getLeagues: async (
    countryCode: string,
    forceRefresh: boolean = false
  ): Promise<BetExplorerLeague[]> => {
    const { data } = await apiClient.get<BetExplorerLeague[]>(
      `/betexplorer/leagues/${countryCode}`,
      { params: { forceRefresh } }
    );
    return data;
  },
};

// Unmatched Leagues API
export const unmatchedLeagueApi = {
  // Get unmatched leagues with optional filters
  getUnmatchedLeagues: async (params?: {
    providerId?: string;
    unresolvedOnly?: boolean;
  }): Promise<UnmatchedLeague[]> => {
    const queryParams = new URLSearchParams();
    if (params?.providerId) queryParams.append("providerId", params.providerId);
    if (params?.unresolvedOnly !== undefined)
      queryParams.append("unresolvedOnly", String(params.unresolvedOnly));

    const url = queryParams.toString()
      ? `/unmatched-leagues?${queryParams.toString()}`
      : "/unmatched-leagues";
    const { data } = await apiClient.get<UnmatchedLeague[]>(url);
    return data;
  },

  // Get single unmatched league
  getById: async (id: string): Promise<UnmatchedLeague> => {
    const { data } = await apiClient.get<UnmatchedLeague>(
      `/unmatched-leagues/${id}`
    );
    return data;
  },

  // Resolve as mapped to existing league
  resolveAsMap: async (
    id: string,
    leagueId: string,
    notes?: string
  ): Promise<{ success: boolean; message: string }> => {
    const { data } = await apiClient.post(`/unmatched-leagues/${id}/resolve/map`, {
      leagueId,
      notes,
    });
    return data;
  },

  // Resolve as ignored
  resolveAsIgnore: async (
    id: string,
    notes?: string
  ): Promise<{ success: boolean; message: string }> => {
    const { data } = await apiClient.post(`/unmatched-leagues/${id}/resolve/ignore`, {
      notes,
    });
    return data;
  },

  // Resolve as unavailable (BetExplorer doesn't support this league)
  resolveAsUnavailable: async (
    id: string,
    notes?: string
  ): Promise<{ success: boolean; message: string }> => {
    const { data } = await apiClient.post(`/unmatched-leagues/${id}/resolve/unavailable`, {
      notes,
    });
    return data;
  },

  // Clear resolution
  unresolve: async (id: string): Promise<{ success: boolean; message: string }> => {
    const { data } = await apiClient.post(`/unmatched-leagues/${id}/unresolve`);
    return data;
  },

  // Delete unmatched league
  delete: async (id: string): Promise<{ success: boolean; message: string }> => {
    const { data } = await apiClient.delete(`/unmatched-leagues/${id}`);
    return data;
  },

  // Get statistics
  getStats: async (): Promise<UnmatchedLeagueStats> => {
    const { data } = await apiClient.get<UnmatchedLeagueStats>(
      "/unmatched-leagues/stats"
    );
    return data;
  },

  // Get mapping suggestions for an unmatched league
  getSuggestions: async (
    id: string
  ): Promise<{ unmatchedLeague: UnmatchedLeague; suggestions: League[] }> => {
    const { data } = await apiClient.get(`/unmatched-leagues/suggestions/${id}`);
    return data;
  },

  // Create new league from BetExplorer and resolve unmatched league
  resolveCreateFromBetExplorer: async (
    id: string,
    betExplorerSlug: string,
    leagueName?: string,
    countryId?: string,
    notes?: string
  ): Promise<{
    success: boolean;
    message: string;
    leagueId: string;
    created: boolean;
  }> => {
    const { data } = await apiClient.post(
      `/unmatched-leagues/${id}/resolve/create-from-betexplorer`,
      { betExplorerSlug, leagueName, countryId, notes }
    );
    return data;
  },

  // Preview copying resolutions from source to target provider
  previewCopyResolutions: async (
    sourceProviderId: string,
    targetProviderId: string
  ): Promise<CopyResolutionsPreviewResponse> => {
    const { data } = await apiClient.post<CopyResolutionsPreviewResponse>(
      "/unmatched-leagues/copy-resolutions/preview",
      { sourceProviderId, targetProviderId }
    );
    return data;
  },

  // Execute copying resolutions from source to target provider
  executeCopyResolutions: async (
    sourceProviderId: string,
    targetProviderId: string
  ): Promise<CopyResolutionsExecuteResponse> => {
    const { data } = await apiClient.post<CopyResolutionsExecuteResponse>(
      "/unmatched-leagues/copy-resolutions/execute",
      { sourceProviderId, targetProviderId }
    );
    return data;
  },

  // Get preview of global rule creation
  getGlobalRulePreview: async (id: string): Promise<GlobalRulePreview> => {
    const { data } = await apiClient.get<GlobalRulePreview>(
      `/unmatched-leagues/${id}/global-rule/preview`
    );
    return data;
  },

  // Create global rule from mapped league
  createGlobalRule: async (
    id: string,
    request?: { resolveAffectedLeagues?: boolean; notes?: string }
  ): Promise<GlobalRuleResult> => {
    const { data } = await apiClient.post<GlobalRuleResult>(
      `/unmatched-leagues/${id}/global-rule/create`,
      request ?? {}
    );
    return data;
  },
};

// Unmatched Countries API
export const unmatchedCountryApi = {
  // Get unmatched countries with optional filters
  getAll: async (params?: {
    providerId?: string;
    unresolvedOnly?: boolean;
  }): Promise<UnmatchedCountry[]> => {
    const queryParams = new URLSearchParams();
    if (params?.providerId) queryParams.append("providerId", params.providerId);
    if (params?.unresolvedOnly !== undefined)
      queryParams.append("unresolvedOnly", String(params.unresolvedOnly));

    const url = queryParams.toString()
      ? `/unmatched-countries?${queryParams.toString()}`
      : "/unmatched-countries";
    const { data } = await apiClient.get<UnmatchedCountry[]>(url);
    return data;
  },

  // Get single unmatched country
  getById: async (id: string): Promise<UnmatchedCountry> => {
    const { data } = await apiClient.get<UnmatchedCountry>(
      `/unmatched-countries/${id}`
    );
    return data;
  },

  // Resolve as mapped to existing country
  resolveAsMap: async (
    id: string,
    countryId: string,
    notes?: string
  ): Promise<{ success: boolean; message: string }> => {
    const { data } = await apiClient.post(`/unmatched-countries/${id}/resolve/map`, {
      countryId,
      notes,
    });
    return data;
  },

  // Resolve as ignored
  resolveAsIgnore: async (
    id: string,
    notes?: string
  ): Promise<{ success: boolean; message: string }> => {
    const { data } = await apiClient.post(`/unmatched-countries/${id}/resolve/ignore`, {
      notes,
    });
    return data;
  },

  // Resolve as unavailable
  resolveAsUnavailable: async (
    id: string,
    notes?: string
  ): Promise<{ success: boolean; message: string }> => {
    const { data } = await apiClient.post(`/unmatched-countries/${id}/resolve/unavailable`, {
      notes,
    });
    return data;
  },

  // Clear resolution
  unresolve: async (id: string): Promise<{ success: boolean; message: string }> => {
    const { data } = await apiClient.post(`/unmatched-countries/${id}/unresolve`);
    return data;
  },

  // Delete unmatched country
  delete: async (id: string): Promise<{ success: boolean; message: string }> => {
    const { data } = await apiClient.delete(`/unmatched-countries/${id}`);
    return data;
  },

  // Get statistics
  getStats: async (providerId?: string): Promise<UnmatchedCountryStats> => {
    const queryParams = providerId ? `?providerId=${providerId}` : "";
    const { data } = await apiClient.get<UnmatchedCountryStats>(
      `/unmatched-countries/stats${queryParams}`
    );
    return data;
  },

  // Get country suggestions for an unmatched country
  getSuggestions: async (
    id: string,
    search?: string
  ): Promise<{ unmatchedCountry: UnmatchedCountry; suggestions: Country[] }> => {
    const queryParams = search ? `?search=${encodeURIComponent(search)}` : "";
    const { data } = await apiClient.get(`/unmatched-countries/suggestions/${id}${queryParams}`);
    return data;
  },
};

// Recipe endpoints
export const recipeApi = {
  // Get all recipes
  getAll: async (): Promise<RecipeListItem[]> => {
    const { data } = await apiClient.get<RecipeListItem[]>("/recipes");
    return data;
  },

  // Get recipe by ID (full detail)
  getById: async (id: string): Promise<ScraperRecipe> => {
    const { data } = await apiClient.get<ScraperRecipe>(`/recipes/${id}`);
    return data;
  },

  // Get recipes by provider and page type
  getByProvider: async (provider: string, pageType: string): Promise<RecipeListItem[]> => {
    const { data } = await apiClient.get<RecipeListItem[]>(
      `/recipes/by-provider/${provider}/${pageType}`
    );
    return data;
  },

  // Create new recipe
  create: async (request: CreateRecipeRequest): Promise<{ id: string }> => {
    const { data } = await apiClient.post<{ id: string }>("/recipes", request);
    return data;
  },

  // Update recipe
  update: async (id: string, request: UpdateRecipeRequest): Promise<{ message: string }> => {
    const { data } = await apiClient.put<{ message: string }>(`/recipes/${id}`, request);
    return data;
  },

  // Delete recipe
  delete: async (id: string): Promise<{ message: string }> => {
    const { data } = await apiClient.delete<{ message: string }>(`/recipes/${id}`);
    return data;
  },

  // Test recipe on a league/season
  test: async (id: string, request: TestRecipeRequest): Promise<TestRecipeResponse> => {
    const { data } = await apiClient.post<TestRecipeResponse>(`/recipes/${id}/test`, request);
    return data;
  },

  // Get recipe statistics
  getStats: async (): Promise<RecipeStats[]> => {
    const { data } = await apiClient.get<RecipeStats[]>("/recipes/stats");
    return data;
  },
};

// Admin endpoints (requires admin role)
export const adminApi = {
  // Get all users
  getAllUsers: async (): Promise<User[]> => {
    const { data } = await apiClient.get<User[]>("/admin/users");
    return data;
  },

  // Get users pending approval
  getPendingUsers: async (): Promise<User[]> => {
    const { data } = await apiClient.get<User[]>("/admin/users/pending");
    return data;
  },

  // Approve a user
  approveUser: async (userId: string): Promise<User> => {
    const { data } = await apiClient.post<User>(`/admin/users/${userId}/approve`);
    return data;
  },

  // Reject (delete) a user
  rejectUser: async (userId: string): Promise<{ message: string }> => {
    const { data } = await apiClient.post<{ message: string }>(`/admin/users/${userId}/reject`);
    return data;
  },

  // Delete a user
  deleteUser: async (userId: string): Promise<{ message: string }> => {
    const { data } = await apiClient.delete<{ message: string }>(`/admin/users/${userId}`);
    return data;
  },

  // Update a user
  updateUser: async (userId: string, request: UpdateUserRequest): Promise<User> => {
    const { data } = await apiClient.patch<User>(`/admin/users/${userId}`, request);
    return data;
  },
};

// Analytics endpoints
export const analyticsApi = {
  execute: async (spec: ViewSpec): Promise<AnalyticsResult> => {
    const { data } = await apiClient.post<AnalyticsResult>("/analytics/execute", spec);
    return data;
  },

  getMetadata: async (): Promise<AnalyticsMetadata> => {
    const { data } = await apiClient.get<AnalyticsMetadata>("/analytics/metadata");
    return data;
  },

  getViews: async (): Promise<AnalyticsViewListItem[]> => {
    const { data } = await apiClient.get<AnalyticsViewListItem[]>("/analytics/views");
    return data;
  },

  getView: async (id: string): Promise<AnalyticsViewDetail> => {
    const { data } = await apiClient.get<AnalyticsViewDetail>(`/analytics/views/${id}`);
    return data;
  },

  createView: async (request: CreateViewRequest): Promise<{ id: string; name: string }> => {
    const { data } = await apiClient.post<{ id: string; name: string }>("/analytics/views", request);
    return data;
  },

  updateView: async (id: string, request: UpdateViewRequest): Promise<{ id: string; name: string }> => {
    const { data } = await apiClient.put<{ id: string; name: string }>(`/analytics/views/${id}`, request);
    return data;
  },

  deleteView: async (id: string): Promise<{ message: string }> => {
    const { data } = await apiClient.delete<{ message: string }>(`/analytics/views/${id}`);
    return data;
  },

  executeView: async (id: string): Promise<AnalyticsResult> => {
    const { data } = await apiClient.post<AnalyticsResult>(`/analytics/views/${id}/execute`);
    return data;
  },

  toggleFavorite: async (id: string): Promise<{ id: string; isFavorite: boolean }> => {
    const { data } = await apiClient.post<{ id: string; isFavorite: boolean }>(`/analytics/views/${id}/favorite`);
    return data;
  },
};

// Add auth interceptor to include token in requests
apiClient.interceptors.request.use((config) => {
  const token = authApi.getToken();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Add response interceptor to handle 401 errors
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      authApi.removeToken();
      // Optionally redirect to login or trigger state update
    }
    return Promise.reject(error);
  }
);

export default apiClient;
