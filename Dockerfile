# Use .NET SDK as the build environment
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the entire repository preserving folder structure
COPY . .

# Restore dependencies for the API project using the full path
RUN dotnet restore "src/VFXRates.API/VFXRates.API.csproj"

# Build the API project without forcing an output directory
RUN dotnet build "src/VFXRates.API/VFXRates.API.csproj" --no-restore -c Release

# Publish the application (this will rebuild if necessary and output to /app/publish)
FROM build AS publish
RUN dotnet publish "src/VFXRates.API/VFXRates.API.csproj" -c Release -o /app/publish

# Use ASP.NET Core runtime for final execution
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

RUN mkdir -p /app/certs
COPY devcert.pfx /app/certs/devcert.pfx

ENTRYPOINT ["dotnet", "VFXRates.API.dll"]
