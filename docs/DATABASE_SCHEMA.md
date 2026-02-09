# DATABASE_SCHEMA.md - Kompletní databázové schéma

## Přehled

PostgreSQL 16 s dvěma schématy:
- **configuration** - Konfigurační data (sporty, země, ligy, sezóny, provideři)
- **data_import** - Importovaná data, provider cache, job tracking

**Konvence:**
- snake_case pro tabulky a sloupce
- UUID primární klíče
- timestamptz pro datetime (UTC)
- JSONB pro komplexní data
- Auto timestamps (created_at, updated_at)

---

## Schema: configuration

### sports
| Column | Type | Constraints |
|--------|------|-------------|
| id | uuid | PK |
| code | varchar(50) | UNIQUE, NOT NULL |
| name | varchar(100) | UNIQUE, NOT NULL |
| is_active | boolean | NOT NULL |
| priority | integer | NOT NULL |
| created_at | timestamptz | NOT NULL |
| updated_at | timestamptz | NOT NULL |

### countries
| Column | Type | Constraints |
|--------|------|-------------|
| id | uuid | PK |
| code | varchar(50) | UNIQUE, NOT NULL |
| name | varchar(100) | UNIQUE, NOT NULL |
| name_cs | varchar(100) | NULL |
| iso_code | text | NOT NULL |
| flag_emoji | varchar(10) | NOT NULL |
| is_active | boolean | NOT NULL, DEFAULT true |
| created_at | timestamptz | NOT NULL |
| updated_at | timestamptz | NOT NULL |

### leagues
| Column | Type | Constraints |
|--------|------|-------------|
| id | uuid | PK |
| sport_id | uuid | FK → sports.id, NOT NULL |
| country_id | uuid | FK → countries.id, NOT NULL |
| name | varchar(200) | NOT NULL |
| name_cs | varchar(200) | NULL |
| display_name | varchar(250) | NOT NULL |
| bet_explorer_slug | varchar(200) | NOT NULL |
| is_active | boolean | NOT NULL |
| is_bettable | boolean | NOT NULL |
| priority | integer | NOT NULL |
| notes | varchar(1000) | NULL |
| created_at | timestamptz | NOT NULL |
| updated_at | timestamptz | NOT NULL |

### seasons
| Column | Type | Constraints |
|--------|------|-------------|
| id | uuid | PK |
| name | varchar(50) | UNIQUE, NOT NULL |
| start_year | integer | NOT NULL |
| end_year | integer | NULL |
| created_at | timestamptz | NOT NULL |
| updated_at | timestamptz | NOT NULL |

### league_seasons
| Column | Type | Constraints |
|--------|------|-------------|
| id | uuid | PK |
| league_id | uuid | FK → leagues.id, NOT NULL |
| season_id | uuid | FK → seasons.id, NOT NULL |
| is_current | boolean | NOT NULL, DEFAULT false |
| has_data | boolean | NOT NULL, DEFAULT false |
| has_odds | boolean | NOT NULL, DEFAULT false |
| sync_enabled | boolean | NOT NULL, DEFAULT false |
| sync_mode | varchar(20) | NOT NULL, DEFAULT 'Historical' |
| no_data_reason | varchar(30) | NULL |
| no_data_note | varchar(500) | NULL |
| last_successful_recipe_id | uuid | NULL |
| last_recipe_tested_at | timestamptz | NULL |
| is_available_on_betexplorer | boolean | NOT NULL, DEFAULT true |
| rounds_count | integer | NOT NULL, DEFAULT 0 |
| matches_count | integer | NOT NULL, DEFAULT 0 |
| last_scraped_at | timestamptz | NULL |
| last_data_sync_at | timestamptz | NULL |
| is_locked | boolean | NOT NULL, DEFAULT false |
| locked_at | timestamptz | NULL |
| last_validated_at | timestamptz | NULL |
| created_at | timestamptz | NOT NULL |
| updated_at | timestamptz | NOT NULL |

**Indexes:** (league_id, season_id) UNIQUE, is_locked

**Enums:**
- `no_data_reason`: "None", "PageNotFound", "NoRoundsFound", "NoResults", "ParsingError", "NetworkError", "PartialData", "NoRecipeFound"
- `sync_mode`: "Historical", "Current"

### data_providers
| Column | Type | Constraints |
|--------|------|-------------|
| id | uuid | PK |
| code | varchar(50) | UNIQUE, NOT NULL |
| name | varchar(100) | NOT NULL |
| type | integer | NOT NULL |
| base_url | varchar(255) | NOT NULL |
| is_active | boolean | NOT NULL, DEFAULT true |
| priority | integer | NOT NULL, DEFAULT 10 |
| has_logo | boolean | NOT NULL, DEFAULT false |
| logo_uploaded_at | timestamptz | NULL |
| configuration | jsonb | NULL |
| credentials | jsonb | NULL |
| scan_capabilities | jsonb | NOT NULL |
| current_season_patterns | jsonb | NOT NULL, DEFAULT '[]' |
| notes | text | NULL |
| created_at | timestamptz | NOT NULL |
| updated_at | timestamptz | NOT NULL |

**Enums (type):** 1=Scraper, 2=API, 3=Manual, 4=BettingProvider

### country_providers
| Column | Type | Constraints |
|--------|------|-------------|
| id | uuid | PK |
| country_id | uuid | FK → countries.id, NOT NULL |
| provider_id | uuid | FK → data_providers.id, NOT NULL |
| provider_code | varchar(100) | NOT NULL |
| provider_name | varchar(200) | NULL |
| is_active | boolean | NOT NULL, DEFAULT true |
| metadata | jsonb | NULL |
| created_at | timestamptz | NOT NULL |
| updated_at | timestamptz | NOT NULL |

**Unique Index:** (country_id, provider_id)

### league_providers
| Column | Type | Constraints |
|--------|------|-------------|
| id | uuid | PK |
| league_id | uuid | FK → leagues.id, NOT NULL |
| provider_id | uuid | FK → data_providers.id, NOT NULL |
| provider_slug | varchar(200) | NOT NULL |
| provider_name | varchar(200) | NULL |
| provider_league_id | integer | NULL |
| is_active | boolean | NOT NULL, DEFAULT true |
| metadata | jsonb | NULL |
| created_at | timestamptz | NOT NULL |
| updated_at | timestamptz | NOT NULL |

**Unique Index:** (league_id, provider_id), (provider_id, provider_slug)

### sport_providers
| Column | Type | Constraints |
|--------|------|-------------|
| id | uuid | PK |
| sport_id | uuid | FK → sports.id, NOT NULL |
| provider_id | uuid | FK → data_providers.id, NOT NULL |
| provider_code | varchar(100) | NOT NULL |
| is_active | boolean | NOT NULL, DEFAULT true |
| metadata | jsonb | NULL |
| created_at | timestamptz | NOT NULL |
| updated_at | timestamptz | NOT NULL |

**Unique Index:** (sport_id, provider_id)

### Identity tabulky (ASP.NET Core)
- `AspNetUsers` - Uživatelé (rozšířeno o display_name, is_approved, language_preference)
- `AspNetRoles` - Role
- `AspNetUserRoles` - User-role mapování
- `AspNetRoleClaims`, `AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserTokens`

---

## Schema: data_import

### rounds
| Column | Type | Constraints |
|--------|------|-------------|
| id | uuid | PK |
| league_id | uuid | FK → configuration.leagues.id, NOT NULL |
| season_id | uuid | FK → configuration.seasons.id, NOT NULL |
| provider_id | uuid | NOT NULL |
| round_number | integer | NOT NULL |
| group_name | varchar(50) | NULL |
| start_date | timestamptz | NULL |
| end_date | timestamptz | NULL |
| matches_count | integer | NOT NULL |
| home_wins | integer | NOT NULL |
| draws | integer | NOT NULL |
| away_wins | integer | NOT NULL |
| cumulative_odds_home | decimal(18,4) | NOT NULL, DEFAULT 1.0 |
| cumulative_odds_draw | decimal(18,4) | NOT NULL, DEFAULT 1.0 |
| cumulative_odds_away | decimal(18,4) | NOT NULL, DEFAULT 1.0 |
| summary_result | varchar(50) | NOT NULL |
| odds_complete | varchar(10) | NOT NULL |
| scraped_at | timestamptz | NOT NULL |
| data_source | varchar(100) | NOT NULL |
| created_at | timestamptz | NOT NULL |
| updated_at | timestamptz | NOT NULL |

**Unique Index:** (league_id, season_id, group_name, round_number)

### matches
| Column | Type | Constraints |
|--------|------|-------------|
| id | uuid | PK |
| round_id | uuid | FK → rounds.id, NOT NULL |
| provider_id | uuid | NOT NULL |
| home_team | varchar(200) | NOT NULL |
| away_team | varchar(200) | NOT NULL |
| home_score | integer | NOT NULL |
| away_score | integer | NOT NULL |
| result | varchar(1) | NOT NULL |
| home_odds | decimal(10,2) | NULL |
| draw_odds | decimal(10,2) | NULL |
| away_odds | decimal(10,2) | NULL |
| match_date | timestamptz | NULL |
| provider_url | varchar(500) | NULL |
| created_at | timestamptz | NOT NULL |
| updated_at | timestamptz | NOT NULL |

**Enums (result):** "H", "D", "A"

### provider_countries (cache)
| Column | Type | Constraints |
|--------|------|-------------|
| id | uuid | PK |
| provider_id | uuid | FK → data_providers.id, NOT NULL |
| provider_code | varchar | NOT NULL |
| provider_name | varchar | NOT NULL |
| iso_code | varchar | NULL |
| flag_emoji | varchar | NULL |
| scraped_at | timestamptz | NOT NULL |
| raw_data | jsonb | NULL |
| is_imported | boolean | NOT NULL, DEFAULT false |
| country_id | uuid | FK → countries.id, NULL |
| imported_at | timestamptz | NULL |
| created_at | timestamptz | NOT NULL |
| updated_at | timestamptz | NOT NULL |

### provider_leagues (cache)
| Column | Type | Constraints |
|--------|------|-------------|
| id | uuid | PK |
| provider_id | uuid | FK → data_providers.id, NOT NULL |
| provider_country_id | uuid | NULL |
| country_code | varchar(50) | NULL |
| provider_slug | varchar(500) | NOT NULL |
| provider_name | varchar(500) | NOT NULL |
| display_name | varchar(500) | NULL |
| priority | integer | NOT NULL, DEFAULT 5 |
| is_bettable | boolean | NOT NULL, DEFAULT true |
| mapping_status | integer | NOT NULL, DEFAULT 0 |
| scraped_at | timestamptz | NOT NULL |
| raw_data | jsonb | NULL |
| is_imported | boolean | NOT NULL, DEFAULT false |
| league_id | uuid | FK → leagues.id, NULL |
| imported_at | timestamptz | NULL |
| created_at | timestamptz | NOT NULL |
| updated_at | timestamptz | NOT NULL |

**Enums (mapping_status):** 0=Unmapped, 1=AutoMapped, 2=ManualMapped

### provider_seasons (cache)
| Column | Type | Constraints |
|--------|------|-------------|
| id | uuid | PK |
| provider_id | uuid | FK → data_providers.id, NOT NULL |
| provider_league_id | uuid | FK → provider_leagues.id, NOT NULL |
| season_name | varchar | NOT NULL |
| start_year | integer | NOT NULL |
| end_year | integer | NULL |
| is_current_season | boolean | NOT NULL, DEFAULT false |
| scraped_at | timestamptz | NOT NULL |
| raw_data | jsonb | NULL |
| is_imported | boolean | NOT NULL, DEFAULT false |
| season_id | uuid | FK → seasons.id, NULL |
| imported_at | timestamptz | NULL |
| created_at | timestamptz | NOT NULL |
| updated_at | timestamptz | NOT NULL |

### unmatched_countries
| Column | Type | Constraints |
|--------|------|-------------|
| id | uuid | PK |
| provider_id | uuid | FK → data_providers.id, NOT NULL |
| provider_country_id | varchar(100) | NULL |
| provider_country_name | varchar | NOT NULL |
| provider_slug | varchar | NULL |
| scraped_at | timestamptz | NOT NULL |
| is_resolved | boolean | NOT NULL, DEFAULT false |
| resolution_type | varchar(20) | NULL |
| resolved_country_id | uuid | FK → countries.id, NULL |
| resolved_at | timestamptz | NULL |
| resolution_notes | varchar(500) | NULL |
| created_at | timestamptz | NOT NULL |
| updated_at | timestamptz | NOT NULL |

**Enums (resolution_type):** "Mapped", "Ignored", "Unavailable", "ManuallyMapped"

### unmatched_leagues
| Column | Type | Constraints |
|--------|------|-------------|
| id | uuid | PK |
| provider_id | uuid | FK → data_providers.id, NOT NULL |
| provider_league_id | varchar(100) | NULL |
| provider_league_name | varchar(200) | NOT NULL |
| provider_slug | varchar(200) | NULL |
| country_code | varchar(50) | NOT NULL |
| country_name | varchar(100) | NULL |
| scraped_at | timestamptz | NOT NULL |
| is_resolved | boolean | NOT NULL, DEFAULT false |
| resolution_type | varchar(20) | NULL |
| resolved_league_id | uuid | FK → leagues.id, NULL |
| resolved_at | timestamptz | NULL |
| resolution_notes | varchar(500) | NULL |
| created_at | timestamptz | NOT NULL |
| updated_at | timestamptz | NOT NULL |

**Unique Index:** (provider_id, provider_league_name, country_code)

**Enums (resolution_type):** "Mapped", "Ignored", "Unavailable", "ManuallyMapped"

### country_name_mappings
| Column | Type | Constraints |
|--------|------|-------------|
| id | uuid | PK |
| provider_code | varchar | NOT NULL |
| provider_country_name | varchar | NOT NULL |
| betexplorer_code | varchar | NOT NULL |
| is_active | boolean | NOT NULL, DEFAULT true |
| priority | integer | NOT NULL, DEFAULT 0 |
| notes | varchar(500) | NULL |
| match_type | varchar | NOT NULL, DEFAULT 'substring' |
| is_case_sensitive | boolean | NOT NULL, DEFAULT false |
| is_special_case | boolean | NOT NULL, DEFAULT false |
| localized_name | varchar | NULL |
| last_used_at | timestamptz | NULL |
| usage_count | integer | NOT NULL, DEFAULT 0 |
| last_provider_country_id | uuid | NULL |
| created_at | timestamptz | NOT NULL |
| updated_at | timestamptz | NOT NULL |

### league_name_mappings
| Column | Type | Constraints |
|--------|------|-------------|
| id | uuid | PK |
| provider_code | varchar(50) | NOT NULL |
| country_code | varchar(50) | NOT NULL |
| provider_league_name | varchar(200) | NOT NULL |
| normalized_provider_league_name | varchar(200) | NOT NULL |
| betexplorer_slug | varchar(200) | NOT NULL |
| is_active | boolean | NOT NULL, DEFAULT true |
| priority | integer | NOT NULL, DEFAULT 0 |
| notes | varchar(500) | NULL |
| last_used_at | timestamptz | NULL |
| usage_count | integer | NOT NULL, DEFAULT 0 |
| last_provider_league_id | uuid | NULL |
| created_at | timestamptz | NOT NULL |
| updated_at | timestamptz | NOT NULL |

**Poznámka:** `provider_code = "*"` = globální pravidlo pro všechny providery

### import_jobs (legacy)
| Column | Type | Constraints |
|--------|------|-------------|
| id | uuid | PK |
| league_id | uuid | FK → leagues.id, NOT NULL |
| provider_id | uuid | NOT NULL |
| type | varchar | NOT NULL |
| status | varchar | NOT NULL |
| season_ids | jsonb | NOT NULL |
| include_without_odds | boolean | NOT NULL |
| started_at | timestamptz | NOT NULL |
| completed_at | timestamptz | NULL |
| progress | jsonb | NOT NULL |
| created_at | timestamptz | NOT NULL |
| updated_at | timestamptz | NOT NULL |

**Enums:**
- `type`: "Historical", "Incremental"
- `status`: "Pending", "Running", "Completed", "Failed", "PartialSuccess"

### sync_jobs
| Column | Type | Constraints |
|--------|------|-------------|
| id | uuid | PK |
| provider_id | uuid | FK → data_providers.id, NOT NULL |
| type | varchar | NOT NULL |
| entity_type | varchar | NOT NULL |
| status | varchar | NOT NULL |
| priority | integer | NOT NULL, DEFAULT 5 |
| started_at | timestamptz | NOT NULL |
| completed_at | timestamptz | NULL |
| scheduled_for | timestamptz | NULL |
| error_message | text | NULL |
| retry_count | integer | NOT NULL, DEFAULT 0 |
| max_retries | integer | NOT NULL, DEFAULT 3 |
| progress_data | jsonb | NULL |
| country_ids | jsonb | NOT NULL |
| league_ids | jsonb | NOT NULL |
| season_ids | jsonb | NOT NULL |
| created_at | timestamptz | NOT NULL |
| updated_at | timestamptz | NOT NULL |

**Enums:**
- `type`: "Scan", "Import", "LiveUpdate"
- `entity_type`: "Countries", "Leagues", "Seasons", "Rounds", "CountriesAndLeagues"
- `status`: "Pending", "Running", "Completed", "CompletedWithWarnings", "PartiallyCompleted", "Failed", "Cancelled"

### scraper_recipes
| Column | Type | Constraints |
|--------|------|-------------|
| id | uuid | PK |
| name | varchar(100) | NOT NULL |
| provider | varchar(50) | NOT NULL |
| page_type | varchar(50) | NOT NULL |
| description | varchar(500) | NULL |
| is_active | boolean | NOT NULL, DEFAULT true |
| priority | integer | NOT NULL, DEFAULT 100 |
| round_header_selector | varchar(500) | NOT NULL |
| match_row_selector | varchar(500) | NOT NULL |
| odds_cell_selector | varchar(500) | NULL |
| group_pattern_regex | varchar(200) | NULL |
| requires_hint | varchar(100) | NULL |
| actions_json | jsonb | NOT NULL, DEFAULT '[]' |
| total_attempts | integer | NOT NULL, DEFAULT 0 |
| successful_attempts | integer | NOT NULL, DEFAULT 0 |
| created_at | timestamptz | NOT NULL |
| updated_at | timestamptz | NOT NULL |

**Unique Index:** (provider, page_type, name)

---

**Poslední aktualizace:** 2026-02-07
