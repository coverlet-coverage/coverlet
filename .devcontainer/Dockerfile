# Use the most recent .NET LTS as the base image
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS base

# Install the current .NET STS release on top of that
COPY --from=mcr.microsoft.com/dotnet/sdk:9.0 /usr/share/dotnet /usr/share/dotnet

# Install the former .NET LTS release on top of that
COPY --from=mcr.microsoft.com/dotnet/sdk:8.0 /usr/share/dotnet /usr/share/dotnet

