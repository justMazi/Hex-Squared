# Use the .NET SDK for building
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build

# Set the working directory
WORKDIR /source

# Copy the entire solution and project files
COPY . .

# Restore NuGet dependencies for the solution
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet restore backend.sln

ARG TARGETARCH

# Build and publish the solution
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish backend.sln -c Release --self-contained false --use-current-runtime -o /app -a ${TARGETARCH/amd64/x64}

# Use the .NET runtime for running the application
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app

# Copy the published application files
COPY --from=build /app .

# Expose the port (optional, depends on your service configuration)
EXPOSE 8000

# Set the entry point to the main application DLL
ENTRYPOINT ["dotnet", "HexSquared.dll"]
