# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution file
COPY Sazkomat.sln ./

# Copy only production project files (tests not needed for API build)
COPY src/Sazkomat.Core/*.csproj ./src/Sazkomat.Core/
COPY src/Sazkomat.Configuration/*.csproj ./src/Sazkomat.Configuration/
COPY src/Sazkomat.DataImport/*.csproj ./src/Sazkomat.DataImport/
COPY src/Sazkomat.BettingProviders/*.csproj ./src/Sazkomat.BettingProviders/
COPY src/Sazkomat.Strategy/*.csproj ./src/Sazkomat.Strategy/
COPY src/Sazkomat.Api/*.csproj ./src/Sazkomat.Api/

# Restore dependencies with BuildKit cache mount (only API and dependencies)
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore src/Sazkomat.Api/Sazkomat.Api.csproj

# Copy everything else
COPY . .

# Build the API project
WORKDIR /src/src/Sazkomat.Api
RUN dotnet build -c Release -o /app/build

# Publish Stage
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Install Playwright browsers only (no system deps needed in build stage)
ENV PLAYWRIGHT_BROWSERS_PATH=/ms-playwright
RUN --mount=type=cache,target=/root/.dotnet/tools \
    dotnet tool install --global Microsoft.Playwright.CLI && \
    /root/.dotnet/tools/playwright install chromium

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Install minimal dependencies for Playwright Chromium + curl for healthchecks
RUN --mount=type=cache,target=/var/cache/apt,sharing=locked \
    --mount=type=cache,target=/var/lib/apt,sharing=locked \
    apt-get update && \
    apt-get install -y --no-install-recommends \
    curl \
    # Chromium runtime dependencies (minimal set)
    libnss3 \
    libnspr4 \
    libatk1.0-0 \
    libatk-bridge2.0-0 \
    libcups2 \
    libdrm2 \
    libxkbcommon0 \
    libxcomposite1 \
    libxdamage1 \
    libxfixes3 \
    libxrandr2 \
    libgbm1 \
    libasound2t64 \
    libpango-1.0-0 \
    libcairo2

# Copy published app
COPY --from=publish /app/publish .

# Copy Playwright browsers from build stage
ENV PLAYWRIGHT_BROWSERS_PATH=/ms-playwright
COPY --from=publish /ms-playwright /ms-playwright

# Create logs directory
RUN mkdir -p /app/logs

# Expose port
EXPOSE 3001

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:3001/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "Sazkomat.Api.dll"]
