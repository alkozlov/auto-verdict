FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY AutoVerdict.sln ./
COPY src/AutoVerdict.Api/AutoVerdict.Api.csproj src/AutoVerdict.Api/
RUN dotnet restore src/AutoVerdict.Api/AutoVerdict.Api.csproj
COPY . .
RUN dotnet publish src/AutoVerdict.Api/AutoVerdict.Api.csproj -c Release -o /app/publish --no-restore

FROM runtime AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AutoVerdict.Api.dll"]
