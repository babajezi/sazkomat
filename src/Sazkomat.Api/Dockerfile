# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution file
COPY Sazkomat.sln ./

# Copy all project files
COPY src/Sazkomat.Core/*.csproj ./src/Sazkomat.Core/
COPY src/Sazkomat.Configuration/*.csproj ./src/Sazkomat.Configuration/
COPY src/Sazkomat.DataImport/*.csproj ./src/Sazkomat.DataImport/
COPY src/Sazkomat.BettingProviders/*.csproj ./src/Sazkomat.BettingProviders/
COPY src/Sazkomat.Strategy/*.csproj ./src/Sazkomat.Strategy/
COPY src/Sazkomat.Api/*.csproj ./src/Sazkomat.Api/
COPY tests/Sazkomat.Tests/*.csproj ./tests/Sazkomat.Tests/

# Restore dependencies
RUN dotnet restore

# Copy everything else
COPY . .

# Build the API project
WORKDIR /src/src/Sazkomat.Api
RUN dotnet build -c Release -o /app/build

# Publish Stage
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Install dependencies for Playwright + curl for healthchecks
RUN apt-get update && \
    apt-get install -y \
    curl \
    wget \
    # Playwright Chromium dependencies
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
    libasound2 \
    libpango-1.0-0 \
    libcairo2 \
    && rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=publish /app/publish .

# Install Playwright browsers
ENV PLAYWRIGHT_BROWSERS_PATH=/ms-playwright
RUN pwsh bin/Debug/net9.0/playwright.ps1 install chromium --with-deps || \
    (apt-get update && apt-get install -y dotnet-sdk-9.0 && \
    dotnet tool install --global Microsoft.Playwright.CLI && \
    /root/.dotnet/tools/playwright install chromium --with-deps && \
    rm -rf /var/lib/apt/lists/*)

# Create logs directory
RUN mkdir -p /app/logs

# Expose port
EXPOSE 3001

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:3001/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "Sazkomat.Api.dll"]
