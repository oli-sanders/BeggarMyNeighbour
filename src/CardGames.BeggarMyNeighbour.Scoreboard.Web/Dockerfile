# Build context is the ./src directory so that project references resolve.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish CardGames.BeggarMyNeighbour.Scoreboard.Web/CardGames.BeggarMyNeighbour.Scoreboard.Web.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CardGames.BeggarMyNeighbour.Scoreboard.Web.dll"]
