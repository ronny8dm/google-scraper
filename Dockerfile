# Use Amazon Linux 2 compatible base image for Elastic Beanstalk
FROM mcr.microsoft.com/dotnet/aspnet:9.0-bookworm-slim AS base

# Install required dependencies for Playwright
RUN apt-get update && apt-get install -y \
    wget \
    curl \
    gnupg \
    # X11 and browser dependencies
    xvfb \
    libxss1 \
    libgconf-2-4 \
    libxtst6 \
    libxrandr2 \
    libasound2 \
    libpangocairo-1.0-0 \
    libatk1.0-0 \
    libcairo-gobject2 \
    libgtk-3-0 \
    libgdk-pixbuf2.0-0 \
    libxcomposite1 \
    libxcursor1 \
    libxdamage1 \
    libxi6 \
    libxtst6 \
    libnss3 \
    libcups2 \
    libdrm2 \
    libgtk-3-0 \
    libxss1 \
    # Fonts
    fonts-liberation \
    fonts-dejavu-core \
    --no-install-recommends \
    && rm -rf /var/lib/apt/lists/*

# Install Node.js and Playwright
RUN curl -fsSL https://deb.nodesource.com/setup_18.x | bash - \
    && apt-get install -y nodejs \
    && npm install -g playwright@1.40.0 \
    && playwright install chromium \
    && playwright install-deps chromium

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

# Set environment variables
ENV PLAYWRIGHT_BROWSERS_PATH=/root/.cache/ms-playwright
ENV PLAYWRIGHT_SKIP_BROWSER_DOWNLOAD=1
ENV DISPLAY=:99
ENV ASPNETCORE_URLS=http://+:$PORT
ENV ASPNETCORE_ENVIRONMENT=Production

# Copy published app
COPY --from=publish /app/publish .

# Create startup script with virtual display
RUN echo '#!/bin/bash\n\
# Start virtual display\n\
Xvfb :99 -screen 0 1024x768x16 &\n\
# Start the application\n\
exec dotnet superScrape.dll' > /app/start.sh \
    && chmod +x /app/start.sh

# Set the entry point
ENTRYPOINT ["/app/start.sh"]