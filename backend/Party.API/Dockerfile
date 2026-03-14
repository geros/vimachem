FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend/Party.API/Party.API.csproj backend/Party.API/
COPY backend/Shared/Shared.csproj backend/Shared/
RUN dotnet restore backend/Party.API/Party.API.csproj
COPY backend/ backend/
WORKDIR /src/backend/Party.API
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Party.API.dll"]
