# Build context is the ./src directory so that project references resolve.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish CardGames.BeggarMyNeighbour.Verify/CardGames.BeggarMyNeighbour.Verify.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BeggarVerify.dll"]
