# syntax=docker/dockerfile:1

# Stage 1: Build
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
COPY . /source
WORKDIR /source/HexSquared

# Build the application for the target architecture
ARG TARGETARCH
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish -a ${TARGETARCH/amd64/x64} --use-current-runtime --self-contained false -o /app

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app

# Copy application files from the build stage
COPY --from=build /app .

# Ensure the dotnet command runs correctly using the full path
ENTRYPOINT ["dotnet", "HexSquared.dll"]
