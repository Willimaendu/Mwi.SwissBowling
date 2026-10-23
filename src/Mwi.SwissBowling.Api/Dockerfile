FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/Mwi.SwissBowling.Contracts/ src/Mwi.SwissBowling.Contracts/
COPY src/Mwi.SwissBowling.Core/ src/Mwi.SwissBowling.Core/
COPY src/Mwi.SwissBowling.Api/ src/Mwi.SwissBowling.Api/
RUN dotnet restore src/Mwi.SwissBowling.Api/Mwi.SwissBowling.Api.csproj
RUN dotnet publish src/Mwi.SwissBowling.Api/Mwi.SwissBowling.Api.csproj -c Release --no-restore -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled-extra AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Mwi.SwissBowling.Api.dll"]
