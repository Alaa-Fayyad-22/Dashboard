# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/out ./

# Set environment variable for production (optional, but recommended)
ENV ASPNETCORE_ENVIRONMENT=Production

# Expose port (adjust to your app's port if not 80)
EXPOSE 80

ENTRYPOINT ["dotnet", "UniversalDashboard.dll"]

