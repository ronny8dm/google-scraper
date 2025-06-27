FROM mcr.microsoft.com/playwright/dotnet:v1.52.0-noble

WORKDIR /app

# Copy published files (assuming you've published locally first)
COPY bin/Release/net9.0/publish/ .

# Set environment variables
ENV PLAYWRIGHT_BROWSERS_PATH=/ms-playwright
ENV PORT=8080
ENV ASPNETCORE_URLS="http://+:8080"

# Entry point
CMD ["dotnet", "superScrape.dll"]