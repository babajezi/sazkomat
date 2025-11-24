# 🔐 ASP.NET Core Identity + Language Preferences - Implementation Plan

**Project:** Sazkomat
**Feature:** User Management with OAuth + Per-User Language Preferences
**Start Date:** 2025-11-24
**Status:** 🚧 IN PROGRESS - FÁZE 1 (40%)

---

## 📋 Requirements Summary

- ✅ ASP.NET Core Identity for user management
- ✅ OAuth providers: Google + Local accounts (email/password)
- ✅ Simple roles (just "User" for now, extensible for Admin/Editor later)
- ✅ Hybrid authentication: JWT for API, Cookies for frontend
- ✅ Shared data model (all users see same leagues, countries, matches)
- ✅ Per-user language preference (Czech/English)

---

## 🎯 Implementation Phases

### ✅ FÁZE 1: Backend Identity Foundation (40% COMPLETE)

**Status:** 🚧 IN PROGRESS

#### Completed:
- [x] Added NuGet packages:
  - `Microsoft.AspNetCore.Identity.EntityFrameworkCore` (Configuration project)
  - `Microsoft.AspNetCore.Authentication.JwtBearer` (API project)
  - `Microsoft.AspNetCore.Authentication.Google` (API project)
  - `System.IdentityModel.Tokens.Jwt` (API project)
- [x] Created `LanguagePreference` enum (`src/Sazkomat.Core/Enums/LanguagePreference.cs`)
  - Czech = 0 (default)
  - English = 1
- [x] Created `ApplicationUser` entity (`src/Sazkomat.Configuration/Entities/ApplicationUser.cs`)
  - Extends `IdentityUser`
  - Properties: LanguagePreference, DisplayName, CreatedAt, UpdatedAt

#### To Do:
- [ ] Create `ApplicationUserConfiguration` (EF Fluent API)
  - File: `src/Sazkomat.Configuration/Data/Configurations/ApplicationUserConfiguration.cs`
  - Configure custom properties with snake_case column names
- [ ] Update `ConfigurationDbContext`
  - Change base class: `DbContext` → `IdentityDbContext<ApplicationUser>`
  - Add Identity DbSets
  - Preserve existing entities (Country, League, Sport, etc.)
- [ ] Create EF Migration: `AddIdentity`
  - Tables: AspNetUsers, AspNetRoles, AspNetUserClaims, AspNetUserLogins, AspNetUserTokens, AspNetRoleClaims
  - Custom columns: language_preference, display_name, created_at, updated_at
- [ ] Test build: `dotnet build`

**Deliverable:** Backend ready for Identity with migration

---

### ⏳ FÁZE 2: Authentication Service & JWT

**Status:** 🔜 PENDING

#### To Implement:
- [ ] Create `JwtSettings` configuration class
  - Properties: SecretKey, Issuer, Audience, ExpirationMinutes
- [ ] Create `IAuthService` interface
  - Methods: GenerateJwtToken, Register, Login, ValidateToken
- [ ] Implement `AuthService` class
  - JWT token generation with claims (UserId, Email, LanguagePreference)
  - User registration with password hashing
  - User login with credentials validation
- [ ] Create DTOs:
  - `RegisterRequest` (Email, Password, DisplayName?, LanguagePreference?)
  - `LoginRequest` (Email, Password)
  - `AuthResponse` (Token, ExpiresAt, User info)
- [ ] Add JwtSettings to `appsettings.json`
- [ ] Unit test AuthService in isolation

**Deliverable:** Functional authentication service with JWT generation

---

### ⏳ FÁZE 3: Auth API Endpoints

**Status:** 🔜 PENDING

#### To Implement:
- [ ] Create `AuthEndpoints.cs`
- [ ] Implement endpoints:
  - `POST /api/auth/register` - User registration
  - `POST /api/auth/login` - Login (returns JWT + sets HttpOnly cookie)
  - `GET /api/auth/me` - Get current user info (requires auth)
  - `PATCH /api/auth/me/language` - Update language preference
  - `POST /api/auth/logout` - Logout (clear cookie)
- [ ] Configure `Program.cs`:
  - `AddIdentity<ApplicationUser, IdentityRole>()`
  - `AddAuthentication()` with JwtBearer + Cookies
  - `AddJwtBearer()` with token validation
  - `UseAuthentication()` + `UseAuthorization()`
- [ ] Test endpoints with curl/Postman

**Deliverable:** Functional Auth API endpoints

---

### ⏳ FÁZE 4: Google OAuth Integration

**Status:** 🔜 PENDING

#### To Implement:
- [ ] Add Google OAuth config to `appsettings.json`:
  ```json
  "Authentication": {
    "Google": {
      "ClientId": "YOUR_CLIENT_ID",
      "ClientSecret": "YOUR_CLIENT_SECRET"
    }
  }
  ```
- [ ] Create Google Cloud Console project (if not exists)
- [ ] Get OAuth credentials
- [ ] Implement `POST /api/auth/google` endpoint
  - Handle Google OAuth callback
  - Create/update user from Google profile
  - Generate JWT token
- [ ] Add `AddGoogle()` to Program.cs
- [ ] Test OAuth flow end-to-end

**Deliverable:** Functional Google OAuth login

---

### ⏳ FÁZE 5: Frontend - Auth Context & API Client

**Status:** 🔜 PENDING

#### To Implement:
- [ ] Create TypeScript types (`frontend/lib/api/types.ts`):
  - `User` interface
  - `AuthResponse` interface
  - `LoginRequest`, `RegisterRequest` types
- [ ] Implement `authApi` client (`frontend/lib/api/client.ts`):
  - `login(credentials)`
  - `register(data)`
  - `logout()`
  - `getMe()`
  - `updateLanguage(language)`
- [ ] Create `UserContext` provider (`frontend/contexts/UserContext.tsx`):
  - State: user, isAuthenticated, isLoading
  - Methods: login, logout, updateLanguage
  - Load user on mount from `/api/auth/me`
- [ ] Create `useUser()` hook
- [ ] Integrate into `frontend/app/layout.tsx`

**Deliverable:** Frontend context ready for auth

---

### ⏳ FÁZE 6: Frontend - Auth UI Components

**Status:** 🔜 PENDING

#### To Implement:
- [ ] `LoginDialog.tsx` - Login form with email/password
  - Google login button
  - Link to register
- [ ] `RegisterDialog.tsx` - Registration form
  - Email, password, display name
  - Language preference selector
- [ ] `GoogleLoginButton.tsx` - Google OAuth button
  - Redirect to `/api/auth/google`
- [ ] `UserMenu.tsx` - Dropdown with user info
  - Display name / email
  - Language selector
  - Logout button
- [ ] Style components with shadcn/ui

**Deliverable:** Functional UI for login and registration

---

### ⏳ FÁZE 7: Language Selector & Helper Updates

**Status:** 🔜 PENDING

#### To Implement:
- [ ] `LanguageSelector.tsx` component
  - Dropdown with Czech/English options
  - Updates user preference via API
  - Real-time UI update
- [ ] Update `getCountryDisplayName()`:
  - Add `language` parameter (read from UserContext)
  - Return `nameCs` if language=Czech, else `name`
- [ ] Update `getLeagueDisplayName()`:
  - Add `language` parameter (read from UserContext)
  - Return `nameCs` if language=Czech, else `displayName`
- [ ] Create `useLanguage()` hook:
  - Returns current language from UserContext
  - Provides `changeLanguage()` function
- [ ] Update all components to use `useLanguage()`

**Deliverable:** Functional language switching with real-time updates

---

### ⏳ FÁZE 8: Integration & Navigation

**Status:** 🔜 PENDING

#### To Implement:
- [ ] Add `UserMenu` to main navigation (`frontend/components/Navigation.tsx` or similar)
- [ ] Add `LanguageSelector` to UserMenu or header
- [ ] Implement protected routes (optional):
  - Middleware to check authentication
  - Redirect to login if not authenticated
- [ ] E2E testing:
  - Register new user
  - Login with email/password
  - Login with Google
  - Change language preference
  - Verify UI updates (country/league names)
  - Logout
- [ ] Bug fixes and polish
- [ ] Update documentation

**Deliverable:** Fully functional system with auth + language preferences

---

## 📊 Progress Tracking

| Phase | Status | Progress | ETA |
|-------|--------|----------|-----|
| Fáze 1 | 🚧 In Progress | 40% | Current |
| Fáze 2 | 🔜 Pending | 0% | After Fáze 1 |
| Fáze 3 | 🔜 Pending | 0% | After Fáze 2 |
| Fáze 4 | 🔜 Pending | 0% | After Fáze 3 |
| Fáze 5 | 🔜 Pending | 0% | After Fáze 4 |
| Fáze 6 | 🔜 Pending | 0% | After Fáze 5 |
| Fáze 7 | 🔜 Pending | 0% | After Fáze 6 |
| Fáze 8 | 🔜 Pending | 0% | After Fáze 7 |

**Overall Progress:** 5% (1/8 phases, Fáze 1 at 40%)

---

## 🔧 Configuration Notes

### appsettings.json additions needed:

```json
{
  "JwtSettings": {
    "SecretKey": "YOUR_SECRET_KEY_MIN_32_CHARS",
    "Issuer": "Sazkomat",
    "Audience": "SazkomatUsers",
    "ExpirationMinutes": 1440
  },
  "Authentication": {
    "Google": {
      "ClientId": "YOUR_GOOGLE_CLIENT_ID",
      "ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET"
    }
  }
}
```

### Google OAuth Setup:
1. Go to: https://console.cloud.google.com/
2. Create new project or select existing
3. Enable Google+ API
4. Create OAuth 2.0 credentials
5. Add authorized redirect URI: `https://yourdomain.com/api/auth/google/callback`
6. Copy Client ID and Secret to appsettings.json

---

## 📝 After Each Phase:

1. ✅ Commit changes: `git add . && git commit -m "feat: Complete Fáze X - <description>"`
2. ✅ Test functionality
3. ✅ Update this document (mark checkboxes, update progress)
4. ✅ Reset context if needed
5. ✅ Continue with next phase

---

## 🚀 Quick Commands

```bash
# Build solution
dotnet build

# Create migration
dotnet ef migrations add MigrationName --project src/Sazkomat.Configuration --startup-project src/Sazkomat.Api

# Apply migration
dotnet ef database update --project src/Sazkomat.Configuration --startup-project src/Sazkomat.Api

# Run API
cd src/Sazkomat.Api && dotnet run

# Run Frontend
cd frontend && npm run dev
```

---

**Last Updated:** 2025-11-24
**Current Focus:** Completing Fáze 1 - Backend Identity Foundation
