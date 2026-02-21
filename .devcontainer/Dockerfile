# Use the most recent .NET LTS as the base image
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS base

# Install the current .NET STS release SDK and shared frameworks side-by-side
COPY --from=mcr.microsoft.com/dotnet/sdk:9.0 /usr/share/dotnet/sdk /usr/share/dotnet/sdk
COPY --from=mcr.microsoft.com/dotnet/sdk:9.0 /usr/share/dotnet/shared /usr/share/dotnet/shared

# Install the former .NET LTS release SDK and shared frameworks side-by-side
COPY --from=mcr.microsoft.com/dotnet/sdk:8.0 /usr/share/dotnet/sdk /usr/share/dotnet/sdk
COPY --from=mcr.microsoft.com/dotnet/sdk:8.0 /usr/share/dotnet/shared /usr/share/dotnet/shared
