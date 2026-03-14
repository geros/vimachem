FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first for better layer caching
COPY backend/Shared/Shared.csproj backend/Shared/
COPY backend/Lending.API/Lending.API.csproj backend/Lending.API/
RUN dotnet restore backend/Lending.API/Lending.API.csproj

# Copy everything else and build
COPY . .
RUN dotnet publish backend/Lending.API/Lending.API.csproj -c Release -o /app/publish

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Lending.API.dll"]
