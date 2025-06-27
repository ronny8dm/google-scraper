FROM mcr.microsoft.com/playwright/dotnet:v1.40.0-noble AS base

# Install additional tools and latest .NET 9
RUN apt-get update && apt-get install -y \
    curl \
    telnet \
    procps \
    iputils-ping \
    --no-install-recommends \
    && curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --install-dir /usr/share/dotnet --channel 9.0 \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . .
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app

# Set environment variables for Playwright
ENV PLAYWRIGHT_BROWSERS_PATH=/ms-playwright
# Use Heroku's PORT environment variable
ENV ASPNETCORE_URLS=http://+:$PORT

# Copy published app
COPY --from=publish /app/publish .

# Verify Playwright installation
RUN pwsh -Command "Write-Host 'Verifying Playwright installation:'; Get-ChildItem -Path /ms-playwright -Recurse -Depth 1"

# Set the entry point
ENTRYPOINT ["dotnet", "superScrape.dll"]