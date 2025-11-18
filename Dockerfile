# ============================================
# Dockerfile para DondeSalimos.Server
# Estructura: DondeSalimosAPI/DondeSalimos/Server/
# ============================================

# ============================================
# Stage 1: Build
# ============================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar TODO el contenido del repositorio
COPY . .

# Navegar a la carpeta del proyecto Server
WORKDIR /src/DondeSalimosAPI/DondeSalimos/Server

# Restaurar dependencias
RUN dotnet restore DondeSalimos.Server.csproj

# Compilar
RUN dotnet build DondeSalimos.Server.csproj -c Release -o /app/build

# Publicar
RUN dotnet publish DondeSalimos.Server.csproj -c Release -o /app/publish --no-restore

# ============================================
# Stage 2: Runtime
# ============================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copiar archivos publicados desde el stage de build
COPY --from=build /app/publish .

# Verificar que el archivo DLL existe (para debugging)
RUN ls -la

# Exponer puerto (Railway lo asigna dinámicamente)
EXPOSE 8080

# Variables de entorno
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Comando de inicio
ENTRYPOINT ["dotnet", "DondeSalimos.Server.dll"]
