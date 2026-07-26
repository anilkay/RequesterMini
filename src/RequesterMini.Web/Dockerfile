# Build context is the repository root, not this folder:
#   docker build -f src/RequesterMini.Web/Dockerfile -t requestermini-web .
# The web app references seven sibling libraries, so the whole src tree has to be reachable.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Project files first: this layer is cached until a csproj changes, so code edits skip the restore.
COPY src/RequesterMini.Web/RequesterMini.Web.csproj src/RequesterMini.Web/
COPY src/AppLogger/AppLogger.csproj src/AppLogger/
COPY src/BrunoImporter/BrunoImporter.csproj src/BrunoImporter/
COPY src/CurlExporter/CurlExporter.csproj src/CurlExporter/
COPY src/HttpAuth/HttpAuth.csproj src/HttpAuth/
COPY src/HttpRequesting/HttpRequesting.csproj src/HttpRequesting/
COPY src/JsonFileStore/JsonFileStore.csproj src/JsonFileStore/
COPY src/SyntaxHighlighter/SyntaxHighlighter.csproj src/SyntaxHighlighter/
COPY src/UrlQuery/UrlQuery.csproj src/UrlQuery/
RUN dotnet restore src/RequesterMini.Web/RequesterMini.Web.csproj

COPY src/RequesterMini.Web/ src/RequesterMini.Web/
COPY src/AppLogger/ src/AppLogger/
COPY src/BrunoImporter/ src/BrunoImporter/
COPY src/CurlExporter/ src/CurlExporter/
COPY src/HttpAuth/ src/HttpAuth/
COPY src/HttpRequesting/ src/HttpRequesting/
COPY src/JsonFileStore/ src/JsonFileStore/
COPY src/SyntaxHighlighter/ src/SyntaxHighlighter/
COPY src/UrlQuery/ src/UrlQuery/

RUN dotnet publish src/RequesterMini.Web/RequesterMini.Web.csproj \
    -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Request history and logs live here; mount a volume to keep them across container restarts.
RUN mkdir -p /data && chown -R $APP_UID /data
VOLUME ["/data"]

ENV DataDirectory=/data \
    ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "RequesterMini.Web.dll"]
