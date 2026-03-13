FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS base
WORKDIR /app
EXPOSE 5400

FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src
COPY ["src/Audit.API/Audit.API.csproj", "src/Audit.API/"]
COPY ["src/Shared/Shared.csproj", "src/Shared/"]
RUN dotnet restore "src/Audit.API/Audit.API.csproj"
COPY . .
WORKDIR "/src/src/Audit.API"
RUN dotnet build "Audit.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Audit.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Audit.API.dll"]
