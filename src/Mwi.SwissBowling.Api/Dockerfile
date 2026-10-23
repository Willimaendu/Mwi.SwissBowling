FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:0d99ebf1ccad15037b5c98c9fe8979d6b8dbac23812feae2277a0b277b5c5ffa AS build
WORKDIR /source
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/Mwi.SwissBowling.Contracts/ src/Mwi.SwissBowling.Contracts/
COPY src/Mwi.SwissBowling.Core/ src/Mwi.SwissBowling.Core/
COPY src/Mwi.SwissBowling.Api/ src/Mwi.SwissBowling.Api/
RUN dotnet restore src/Mwi.SwissBowling.Api/Mwi.SwissBowling.Api.csproj
RUN dotnet publish src/Mwi.SwissBowling.Api/Mwi.SwissBowling.Api.csproj -c Release --no-restore -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0@sha256:8af87eba5e7cdafb1514d7388be287365da97c132509e84dfe252ffd552398ca AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Mwi.SwissBowling.Api.dll"]
