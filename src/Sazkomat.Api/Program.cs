using Microsoft.EntityFrameworkCore;
using Sazkomat.Api.Endpoints;
using Sazkomat.Api.Middleware;
using Sazkomat.Api.Services;
using Sazkomat.Configuration.Data;
using Sazkomat.Configuration.Repositories;
using Sazkomat.Configuration.Services;
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

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(e =>
            e.Properties.ContainsKey("SourceContext") &&
            e.Properties["SourceContext"].ToString().Contains("Sync") ||
            e.Properties["SourceContext"].ToString().Contains("Import") ||
            e.Properties["SourceContext"].ToString().Contains("Scraper") ||
            e.Properties["SourceContext"].ToString().Contains("Provider"))
        .WriteTo.File(
            path: "C:/projects/private/Sazkomat/logs/sync-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30,
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}"))
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
        policy.WithOrigins("http://localhost:3000", "http://localhost:3001")
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
builder.Services.AddScoped<IDatabaseResetService, DatabaseResetService>();
builder.Services.AddScoped<ISyncWorkflowService, SyncWorkflowService>();
builder.Services.AddScoped<IUniversalImportExportService, UniversalImportExportService>();

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

// Use Hangfire Dashboard
var dashboardPath = app.Configuration.GetValue<string>("Hangfire:DashboardPath") ?? "/hangfire";
app.UseHangfireDashboard(dashboardPath, new DashboardOptions
{
    Authorization = Array.Empty<Hangfire.Dashboard.IDashboardAuthorizationFilter>(), // TODO: Add authentication in production
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
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "An error occurred during database migration or seeding");
        throw;
    }
}

app.Run();
