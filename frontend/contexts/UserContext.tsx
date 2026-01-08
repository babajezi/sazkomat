"use client";

import {
  createContext,
  useContext,
  useState,
  useEffect,
  useCallback,
  ReactNode,
} from "react";
import { authApi } from "@/lib/api/client";
import { LanguagePreference } from "@/lib/api/types";
import type {
  User,
  AuthResponse,
  LoginRequest,
  RegisterRequest,
  GoogleLoginRequest,
} from "@/lib/api/types";

interface UserContextType {
  // State
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;
  isAdmin: boolean;

  // Actions
  login: (credentials: LoginRequest) => Promise<AuthResponse>;
  register: (data: RegisterRequest) => Promise<AuthResponse>;
  googleLogin: (data: GoogleLoginRequest) => Promise<AuthResponse>;
  logout: () => Promise<void>;
  updateLanguage: (language: LanguagePreference) => Promise<User>;
  clearError: () => void;
}

const UserContext = createContext<UserContextType | undefined>(undefined);

interface UserProviderProps {
  children: ReactNode;
}

export function UserProvider({ children }: UserProviderProps) {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const isAuthenticated = !!user && user.isApproved;
  const isAdmin = !!user?.isAdmin;

  // Load user on mount if token exists
  useEffect(() => {
    const loadUser = async () => {
      const token = authApi.getToken();
      if (!token) {
        setIsLoading(false);
        return;
      }

      try {
        const userData = await authApi.getMe();
        setUser(userData);
      } catch (err) {
        // Token is invalid or expired
        authApi.removeToken();
        setUser(null);
      } finally {
        setIsLoading(false);
      }
    };

    loadUser();
  }, []);

  const login = useCallback(async (credentials: LoginRequest): Promise<AuthResponse> => {
    setError(null);
    setIsLoading(true);
    try {
      const response = await authApi.login(credentials);
      // Only set user if approved (token will be present)
      if (response.token) {
        setUser(response.user);
      }
      return response;
    } catch (err: any) {
      const message = err.response?.data?.error || "Login failed";
      setError(message);
      throw new Error(message);
    } finally {
      setIsLoading(false);
    }
  }, []);

  const register = useCallback(async (data: RegisterRequest): Promise<AuthResponse> => {
    setError(null);
    setIsLoading(true);
    try {
      const response = await authApi.register(data);
      // Only set user if approved (token will be present)
      if (response.token) {
        setUser(response.user);
      }
      return response;
    } catch (err: any) {
      const message = err.response?.data?.error || "Registration failed";
      setError(message);
      throw new Error(message);
    } finally {
      setIsLoading(false);
    }
  }, []);

  const googleLogin = useCallback(async (data: GoogleLoginRequest): Promise<AuthResponse> => {
    setError(null);
    setIsLoading(true);
    try {
      const response = await authApi.googleLogin(data);
      // Only set user if approved (token will be present)
      if (response.token) {
        setUser(response.user);
      }
      return response;
    } catch (err: any) {
      const message = err.response?.data?.error || "Google login failed";
      setError(message);
      throw new Error(message);
    } finally {
      setIsLoading(false);
    }
  }, []);

  const logout = useCallback(async (): Promise<void> => {
    setIsLoading(true);
    try {
      await authApi.logout();
    } finally {
      setUser(null);
      setIsLoading(false);
    }
  }, []);

  const updateLanguage = useCallback(async (language: LanguagePreference): Promise<User> => {
    setError(null);
    try {
      const updatedUser = await authApi.updateLanguage({ languagePreference: language });
      setUser(updatedUser);
      return updatedUser;
    } catch (err: any) {
      const message = err.response?.data?.error || "Failed to update language";
      setError(message);
      throw new Error(message);
    }
  }, []);

  const clearError = useCallback(() => {
    setError(null);
  }, []);

  const value: UserContextType = {
    user,
    isAuthenticated,
    isLoading,
    error,
    isAdmin,
    login,
    register,
    googleLogin,
    logout,
    updateLanguage,
    clearError,
  };

  return <UserContext.Provider value={value}>{children}</UserContext.Provider>;
}

// Hook to use the user context
export function useUser(): UserContextType {
  const context = useContext(UserContext);
  if (context === undefined) {
    throw new Error("useUser must be used within a UserProvider");
  }
  return context;
}

// Convenience hook for just the language preference
// Supports both authenticated users (via API) and guests (via localStorage)
export function useLanguage() {
  const { user, updateLanguage, isAuthenticated } = useUser();
  const [guestLanguage, setGuestLanguage] = useState<LanguagePreference>(
    LanguagePreference.Czech
  );

  // Load guest language from localStorage on mount
  useEffect(() => {
    if (typeof window !== "undefined" && !isAuthenticated) {
      const stored = localStorage.getItem("preferredLanguage") as LanguagePreference | null;
      if (stored && Object.values(LanguagePreference).includes(stored)) {
        setGuestLanguage(stored);
      }
    }
  }, [isAuthenticated]);

  // Listen for language change events (for guest users)
  useEffect(() => {
    if (typeof window === "undefined") return;

    const handleLanguageChange = (event: CustomEvent<LanguagePreference>) => {
      setGuestLanguage(event.detail);
    };

    window.addEventListener("languageChange", handleLanguageChange as EventListener);
    return () => {
      window.removeEventListener("languageChange", handleLanguageChange as EventListener);
    };
  }, []);

  // Use user's preference if authenticated, otherwise use guest preference
  const language: LanguagePreference = isAuthenticated
    ? (user?.languagePreference ?? LanguagePreference.Czech)
    : guestLanguage;

  const changeLanguage = useCallback(
    async (newLanguage: LanguagePreference) => {
      if (isAuthenticated) {
        await updateLanguage(newLanguage);
      } else {
        // For guests, store in localStorage
        localStorage.setItem("preferredLanguage", newLanguage);
        setGuestLanguage(newLanguage);
        // Dispatch event for other components
        window.dispatchEvent(new CustomEvent("languageChange", { detail: newLanguage }));
      }
    },
    [isAuthenticated, updateLanguage]
  );

  return {
    language,
    changeLanguage,
    isAuthenticated,
  };
}
