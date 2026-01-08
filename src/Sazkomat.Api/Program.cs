using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Sazkomat.Api.Endpoints;
using Sazkomat.Api.Middleware;
using Sazkomat.Api.Services;
using Sazkomat.Configuration.Data;
using Sazkomat.Configuration.Entities;
using Sazkomat.Configuration.Repositories;
using Sazkomat.Configuration.Services;
using Sazkomat.Configuration.Settings;
using Sazkomat.DataImport.Data;
using Sazkomat.DataImport.Repositories;
using Sazkomat.DataImport.Scrapers;
using Sazkomat.DataImport.Services;
using Sazkomat.BettingProviders.Scrapers;
using Sazkomat.BettingProviders.Services;
using StackExchange.Redis;
using Serilog;
using Serilog.Events;
using Hangfire;
using Hangfire.PostgreSql;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog with 4 separate log files
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore.Hosting.Diagnostics", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore.Routing.EndpointMiddleware", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore.Cors.Infrastructure.CorsService", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    // Console sink
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    // 1. YYYYMMDD.log - Všechno (master chronologický log)
    .WriteTo.File(
        path: "/app/logs/.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
    // 2. error-YYYYMMDD.log - Jen errory (Level >= Error)
    .WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(e => e.Level >= LogEventLevel.Error)
        .WriteTo.File(
            path: "/app/logs/error-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30,
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}"))
    // 3. sync-YYYYMMDD.log - Synchronizační operace z providerů
    .WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(e =>
            e.Properties.ContainsKey("SourceContext") &&
            (e.Properties["SourceContext"].ToString().Contains("ScanService") ||
             e.Properties["SourceContext"].ToString().Contains("LiveSyncService") ||
             e.Properties["SourceContext"].ToString().Contains("ImportService") ||
             e.Properties["SourceContext"].ToString().Contains("ProviderSyncService")))
        .WriteTo.File(
            path: "/app/logs/sync-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30,
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj}{NewLine}{Exception}"))
    // 4. comm-YYYYMMDD.log - HTTP komunikace a scraping
    .WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(e =>
            e.Properties.ContainsKey("SourceContext") &&
            (e.Properties["SourceContext"].ToString().Contains("HttpClient") ||
             e.Properties["SourceContext"].ToString().Contains("Scraper")))
        .WriteTo.File(
            path: "/app/logs/comm-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30,
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj}{NewLine}{Exception}"))
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Configure JSON serialization to handle circular references
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

// Configure CORS for Next.js frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "http://localhost:3001",
                "http://127.0.0.1:3000",
                "http://127.0.0.1:3001",
                "https://sazkomat.herma.cz")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Configure DbContexts
builder.Services.AddDbContext<ConfigurationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDbContext<DataImportDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// === JWT Settings ===
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()
    ?? throw new InvalidOperationException("JwtSettings not configured");

// === Admin Settings ===
builder.Services.Configure<AdminSettings>(builder.Configuration.GetSection("AdminSettings"));

// === ASP.NET Core Identity ===
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Strict password policy
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredUniqueChars = 4;

    // Lockout policy
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<ConfigurationDbContext>()
.AddDefaultTokenProviders();

// === JWT Authentication ===
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// === Rate Limiting ===
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Strict limit for registration (5 requests per minute per IP)
    options.AddFixedWindowLimiter("auth_register", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });

    // Login limit (10 requests per minute per IP)
    options.AddFixedWindowLimiter("auth_login", opt =>
    {
        opt.PermitLimit = 10;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueLimit = 0;
    });
});

// Register Configuration repositories
builder.Services.AddScoped<ISportRepository, SportRepository>();
builder.Services.AddScoped<ICountryRepository, CountryRepository>();
builder.Services.AddScoped<ILeagueRepository, LeagueRepository>();
builder.Services.AddScoped<ISeasonRepository, SeasonRepository>();
builder.Services.AddScoped<ILeagueSeasonRepository, LeagueSeasonRepository>();
builder.Services.AddScoped<IDataProviderRepository, DataProviderRepository>();
builder.Services.AddScoped<ILeagueProviderRepository, LeagueProviderRepository>();
builder.Services.AddScoped<ICountryProviderRepository, CountryProviderRepository>();
builder.Services.AddScoped<ISyncWorkflowStateRepository, SyncWorkflowStateRepository>();
builder.Services.AddScoped<ILogSettingsRepository, LogSettingsRepository>();

// Register Configuration services
builder.Services.AddScoped<IConfigurationService, ConfigurationService>();
builder.Services.AddScoped<ISeasonService, SeasonService>();
builder.Services.AddScoped<IProviderService, ProviderService>();
builder.Services.AddScoped<IRedisService, RedisService>();
builder.Services.AddScoped<IProviderLogoService, ProviderLogoService>();
builder.Services.AddScoped<IDatabaseResetService, DatabaseResetService>();
builder.Services.AddScoped<ISyncWorkflowService, SyncWorkflowService>();
builder.Services.AddScoped<IUniversalImportExportService, UniversalImportExportService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Register DataImport repositories
builder.Services.AddScoped<IRoundRepository, RoundRepository>();
builder.Services.AddScoped<IMatchRepository, MatchRepository>();
builder.Services.AddScoped<IImportJobRepository, ImportJobRepository>();

// Register Provider Cache repositories
builder.Services.AddScoped<IProviderCountryRepository, ProviderCountryRepository>();
builder.Services.AddScoped<IProviderLeagueRepository, ProviderLeagueRepository>();
builder.Services.AddScoped<IProviderSeasonRepository, ProviderSeasonRepository>();
builder.Services.AddScoped<ISyncJobRepository, SyncJobRepository>();
builder.Services.AddScoped<ILeagueNameMappingRepository, LeagueNameMappingRepository>();
builder.Services.AddScoped<ICountryNameMappingRepository, CountryNameMappingRepository>();
builder.Services.AddScoped<IUnmatchedLeagueRepository, UnmatchedLeagueRepository>();

// Register DataImport scrapers
builder.Services.AddScoped<ILeagueScraper, FootballBetExplorerScraper>();
builder.Services.AddScoped<ISeasonScraper, BetExplorerSeasonScraper>();
builder.Services.AddScoped<ICountryScraper, BetExplorerCountryScraper>();
builder.Services.AddScoped<BetExplorerLeagueMetadataScraper>(); // Concrete class for enrichment service
builder.Services.AddScoped<ILeagueMetadataScraper, BetExplorerLeagueMetadataScraper>();
builder.Services.AddScoped<ScraperFactory>();

// Register DataImport validators
builder.Services.AddScoped<Sazkomat.DataImport.Validators.ILeagueRoundValidator, Sazkomat.DataImport.Validators.BetExplorerRoundValidator>();

// Configure Redis
var redisConnectionString = builder.Configuration.GetConnectionString("RedisConnection") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = ConfigurationOptions.Parse(redisConnectionString);
    configuration.AbortOnConnectFail = false; // Don't fail if Redis is unavailable
    return ConnectionMultiplexer.Connect(configuration);
});

// Register BettingProviders scrapers and services
builder.Services.AddScoped<Sazkomat.BettingProviders.Services.BetanoJsonExtractor>();
builder.Services.AddScoped<BetanoScraper>(); // Register as concrete class for injection
builder.Services.AddScoped<IBettingProviderScraper, BetanoScraper>();
builder.Services.AddScoped<ILeagueMetadataScraper, BetanoLeagueMetadataScraper>();
builder.Services.AddScoped<ICountryScraper, BetanoCountryScraper>();
builder.Services.AddScoped<ISeasonScraper, BetanoSeasonScraper>();
builder.Services.AddScoped<IBetanoFullDataProvider, Sazkomat.BettingProviders.Services.BetanoFullDataProviderAdapter>();

// Register FlareSolverr client for Cloudflare bypass
builder.Services.AddHttpClient<Sazkomat.BettingProviders.Services.FlareSolverrClient>();
builder.Services.AddScoped<Sazkomat.BettingProviders.Services.FlareSolverrClient>();

// Register Tipsport scrapers and services
builder.Services.AddScoped<Sazkomat.BettingProviders.Services.TipsportJsonExtractor>();
builder.Services.AddScoped<Sazkomat.BettingProviders.Scrapers.TipsportScraper>();
builder.Services.AddScoped<ILeagueMetadataScraper, Sazkomat.BettingProviders.Scrapers.TipsportLeagueMetadataScraper>();

// Register Fortuna scrapers (skeleton for future implementation)
builder.Services.AddScoped<ILeagueMetadataScraper, Sazkomat.BettingProviders.Scrapers.FortunaLeagueMetadataScraper>();

// Register BettingProviders services
builder.Services.AddScoped<SyncQueueService>();
builder.Services.AddScoped<BettingProviderOrchestrator>();
builder.Services.AddScoped<MultiSportSyncOrchestrator>();

// Register HTTP clients
// ResilientHttpClient for simple HTTP requests (backup)
builder.Services.AddHttpClient<ResilientHttpClient>();
builder.Services.AddScoped<ResilientHttpClient>();

// PlaywrightHttpClient for JavaScript-rendered pages (primary)
builder.Services.AddScoped<PlaywrightHttpClient>();

// Register PlaywrightHttpClient as the default IHttpClient for scrapers
builder.Services.AddScoped<IHttpClient, PlaywrightHttpClient>();

// Register DataImport services
builder.Services.AddScoped<IImportOrchestrator, ImportOrchestrator>();
builder.Services.AddScoped<ISyncService, ProviderSyncService>();
builder.Services.AddScoped<ISeasonSyncService, SeasonSyncService>();
builder.Services.AddScoped<IScanService, ScanService>();
builder.Services.AddScoped<IImportService, ImportService>();
builder.Services.AddScoped<ILiveSyncService, LiveSyncService>();
builder.Services.AddScoped<ISyncJobProcessor, SyncJobProcessor>();
builder.Services.AddScoped<IBetExplorerEnrichmentService, BetExplorerEnrichmentService>();
builder.Services.AddScoped<ICountryMappingService, CountryMappingService>();

// Register Hangfire background services
builder.Services.AddHostedService<RecurringSyncScheduler>();

// Configure Hangfire
var hangfireConnection = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DefaultConnection string not found");

builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(options =>
        options.UseNpgsqlConnection(hangfireConnection),
        new PostgreSqlStorageOptions
        {
            SchemaName = "hangfire",
            QueuePollInterval = TimeSpan.FromSeconds(15)
        }));

builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = builder.Configuration.GetValue<int>("Hangfire:WorkerCount", 5);
    options.ServerName = $"Sazkomat-{Environment.MachineName}";
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Use middleware
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseSerilogRequestLogging();
app.UseCors();

// Authentication & Authorization middleware
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Use Hangfire Dashboard with authentication in production
var dashboardPath = app.Configuration.GetValue<string>("Hangfire:DashboardPath") ?? "/hangfire";
app.UseHangfireDashboard(dashboardPath, new DashboardOptions
{
    Authorization = app.Environment.IsDevelopment()
        ? Array.Empty<Hangfire.Dashboard.IDashboardAuthorizationFilter>()
        : new[] { new HangfireAuthorizationFilter() },
    StatsPollingInterval = 5000, // 5 seconds
    DisplayStorageConnectionString = false
});

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    version = "1.0.0"
}))
.WithName("HealthCheck")
.Produces(200);

// Map endpoints
app.MapConfigurationEndpoints();
app.MapProviderEndpoints();
app.MapProviderLogoEndpoints();
app.MapImportEndpoints();
app.MapSeasonEndpoints();
app.MapDatabaseEndpoints();
app.MapSyncEndpoints();
app.MapImportExportEndpoints();
app.MapScanEndpoints();
app.MapJobEndpoints();
app.MapLiveSyncEndpoints();
app.MapProviderCacheEndpoints();
app.MapLeagueNameMappingEndpoints();
app.MapCountryNameMappingEndpoints();
app.MapUnmatchedLeagueEndpoints();
app.MapBetExplorerEndpoints();
app.MapAuthEndpoints();
app.MapUserAdminEndpoints();
app.MapTipsportEndpoints();

// Auto migration and seed on startup
using (var scope = app.Services.CreateScope())
{
    try
    {
        var configContext = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
        var dataImportContext = scope.ServiceProvider.GetRequiredService<DataImportDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("Running database migrations...");

        // Apply migrations
        await configContext.Database.MigrateAsync();
        await dataImportContext.Database.MigrateAsync();

        logger.LogInformation("Database migrations completed");

        // Seed data
        logger.LogInformation("Seeding configuration data...");
        await ConfigurationSeeder.SeedAsync(configContext);
        logger.LogInformation("Configuration data seeded successfully");

        // Seed country name mappings
        logger.LogInformation("Seeding country name mappings...");
        await CountryNameMappingSeeder.SeedTipsportMappingsAsync(dataImportContext);
        logger.LogInformation("Country name mappings seeded successfully");

        // Cleanup orphaned jobs (stuck in Running status from previous crash/restart)
        logger.LogInformation("Checking for orphaned jobs...");
        var orphanedJobsCount = await dataImportContext.SyncJobs
            .Where(j => j.Status == Sazkomat.DataImport.Entities.SyncJobStatus.Running)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(j => j.Status, Sazkomat.DataImport.Entities.SyncJobStatus.Failed)
                .SetProperty(j => j.CompletedAt, DateTime.UtcNow)
                .SetProperty(j => j.ErrorMessage, "Job orphaned after application restart"));
        if (orphanedJobsCount > 0)
        {
            logger.LogWarning("Cleaned up {Count} orphaned job(s) that were stuck in Running status", orphanedJobsCount);
        }
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "An error occurred during database migration or seeding");
        throw;
    }
}

app.Run();
