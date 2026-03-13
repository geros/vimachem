FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first for better layer caching
COPY src/Shared/Shared.csproj src/Shared/
COPY src/Lending.API/Lending.API.csproj src/Lending.API/
RUN dotnet restore src/Lending.API/Lending.API.csproj

# Copy everything else and build
COPY . .
RUN dotnet publish src/Lending.API/Lending.API.csproj -c Release -o /app/publish

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Lending.API.dll"]
