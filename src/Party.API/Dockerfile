FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Party.API/Party.API.csproj src/Party.API/
COPY src/Shared/Shared.csproj src/Shared/
RUN dotnet restore src/Party.API/Party.API.csproj
COPY src/ src/
WORKDIR /src/src/Party.API
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Party.API.dll"]
