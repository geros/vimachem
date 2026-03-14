FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5400

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["backend/Audit.API/Audit.API.csproj", "backend/Audit.API/"]
COPY ["backend/Shared/Shared.csproj", "backend/Shared/"]
RUN dotnet restore "backend/Audit.API/Audit.API.csproj"
COPY . .
WORKDIR "/src/backend/Audit.API"
RUN dotnet build "Audit.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Audit.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Audit.API.dll"]
