FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY AutoVerdict.sln ./
COPY src/AutoVerdict.ProcessingService/AutoVerdict.ProcessingService.csproj src/AutoVerdict.ProcessingService/
RUN dotnet restore src/AutoVerdict.ProcessingService/AutoVerdict.ProcessingService.csproj
COPY . .
RUN dotnet publish src/AutoVerdict.ProcessingService/AutoVerdict.ProcessingService.csproj -c Release -o /app/publish --no-restore

FROM runtime AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AutoVerdict.ProcessingService.dll"]
