FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY AutoVerdict.sln ./
COPY src/AutoVerdict.Api/AutoVerdict.Api.csproj                         src/AutoVerdict.Api/
COPY src/AutoVerdict.Application/AutoVerdict.Application.csproj         src/AutoVerdict.Application/
COPY src/AutoVerdict.Contracts/AutoVerdict.Contracts.csproj             src/AutoVerdict.Contracts/
COPY src/AutoVerdict.Domain/AutoVerdict.Domain.csproj                   src/AutoVerdict.Domain/
COPY src/AutoVerdict.Infrastructure/AutoVerdict.Infrastructure.csproj   src/AutoVerdict.Infrastructure/
COPY src/AutoVerdict.ProcessingService/AutoVerdict.ProcessingService.csproj src/AutoVerdict.ProcessingService/
RUN dotnet restore AutoVerdict.sln
COPY . .
RUN dotnet publish src/AutoVerdict.ProcessingService/AutoVerdict.ProcessingService.csproj -c Release -o /app/publish --no-restore

FROM runtime AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AutoVerdict.ProcessingService.dll"]
