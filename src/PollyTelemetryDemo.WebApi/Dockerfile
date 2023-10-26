FROM mcr.microsoft.com/dotnet/sdk:7.0-bullseye-slim AS build-env
WORKDIR /app

# Copy everything
COPY . ./src/

# Restore, build and publish using publish command
RUN dotnet publish "./src/PollyTelemetryDemo.WebApi.csproj" \
    -c Release \ 
    -o /app/publish

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:7.0-bullseye-slim

# Copy artifact
WORKDIR /app
COPY --from=build-env /app/publish .

# Starts on port 8080
ENV ASPNETCORE_URLS=http://+:8080

# Set entrypoint
ENTRYPOINT ["dotnet", "PollyTelemetryDemo.WebApi.dll"]