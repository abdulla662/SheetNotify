FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY SheetNotify.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app .
# Mount your service-account.json via Docker volume:
# docker run -v /path/to/service-account.json:/app/service-account.json sheetnotify
ENTRYPOINT ["dotnet", "SheetNotify.dll"]
