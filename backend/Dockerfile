# Stage 1: Build
FROM --platform=linux/arm64 mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build

WORKDIR /source
COPY . .

# Clear NuGet cache and restore dependencies with detailed logging
RUN dotnet nuget locals all --clear
RUN dotnet restore backend.sln --verbosity detailed

# Build and publish the solution
RUN dotnet publish backend.sln -c Release --self-contained false --use-current-runtime -o /app

# Stage 2: Runtime
FROM --platform=linux/arm64 mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime

WORKDIR /app
COPY --from=build /app .
EXPOSE 8000
ENTRYPOINT ["dotnet", "HexSquared.dll"]
