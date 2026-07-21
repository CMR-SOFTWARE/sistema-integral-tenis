# Build context: la carpeta backend/ (Railway → Root Directory = backend)

# ── Etapa de build ──
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiamos primero solo el .csproj para cachear el "restore" entre builds
COPY src/SistemaIntegralDeportivo.Api/SistemaIntegralDeportivo.Api.csproj src/SistemaIntegralDeportivo.Api/
RUN dotnet restore src/SistemaIntegralDeportivo.Api/SistemaIntegralDeportivo.Api.csproj

# Ahora el resto del código y publicamos en Release
COPY src/SistemaIntegralDeportivo.Api/ src/SistemaIntegralDeportivo.Api/
RUN dotnet publish src/SistemaIntegralDeportivo.Api/SistemaIntegralDeportivo.Api.csproj \
    -c Release -o /app --no-restore

# ── Etapa de runtime (imagen liviana, sin el SDK completo) ──
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# Railway inyecta la variable PORT en runtime; ASP.NET Core escucha ahí
# (forma shell del CMD para que $PORT se expanda al arrancar el contenedor)
ENV ASPNETCORE_ENVIRONMENT=Production
CMD ASPNETCORE_URLS=http://+:$PORT dotnet SistemaIntegralDeportivo.Api.dll
