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
  SyncLeaguesRequest,
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

  deleteLeague: async (id: string): Promise<void> => {
    await apiClient.delete(`/config/leagues/${id}`);
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
    config: { timeout?: number; proxyUrl?: string; customSettings?: Record<string, string> }
  ): Promise<DataProvider> => {
    const { data } = await apiClient.patch(`/config/providers/${providerId}/configuration`, config);
    return data;
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
};

export default apiClient;
