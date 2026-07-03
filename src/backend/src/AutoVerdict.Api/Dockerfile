FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/AutoVerdict.Api/AutoVerdict.Api.csproj                         src/AutoVerdict.Api/
COPY src/AutoVerdict.Application/AutoVerdict.Application.csproj         src/AutoVerdict.Application/
COPY src/AutoVerdict.Contracts/AutoVerdict.Contracts.csproj             src/AutoVerdict.Contracts/
COPY src/AutoVerdict.Domain/AutoVerdict.Domain.csproj                   src/AutoVerdict.Domain/
COPY src/AutoVerdict.Infrastructure/AutoVerdict.Infrastructure.csproj   src/AutoVerdict.Infrastructure/
COPY src/AutoVerdict.ProcessingService/AutoVerdict.ProcessingService.csproj src/AutoVerdict.ProcessingService/
RUN dotnet restore src/AutoVerdict.Api/AutoVerdict.Api.csproj
COPY . .
RUN dotnet publish src/AutoVerdict.Api/AutoVerdict.Api.csproj -c Release -o /app/publish --no-restore

FROM runtime AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AutoVerdict.Api.dll"]
