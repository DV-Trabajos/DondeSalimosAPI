# ============================================
# Dockerfile para DondeSalimos.Server
# Estructura: DondeSalimosAPI/DondeSalimos/Server/
# ============================================

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar archivos de solución y proyectos
COPY DondeSalimosAPI/DondeSalimos/Server/*.csproj ./DondeSalimosAPI/DondeSalimos/Server/
COPY DondeSalimosAPI/DondeSalimos/Shared/*.csproj ./DondeSalimosAPI/DondeSalimos/Shared/

# Restaurar dependencias
RUN dotnet restore ./DondeSalimosAPI/DondeSalimos/Server/DondeSalimos.Server.csproj

# Copiar el resto de archivos
COPY DondeSalimosAPI/DondeSalimos/Server/ ./DondeSalimosAPI/DondeSalimos/Server/
COPY DondeSalimosAPI/DondeSalimos/Shared/ ./DondeSalimosAPI/DondeSalimos/Shared/

# Construir
WORKDIR /src/DondeSalimosAPI/DondeSalimos/Server
RUN dotnet build DondeSalimos.Server.csproj -c Release -o /app/build

# Publicar
FROM build AS publish
RUN dotnet publish DondeSalimos.Server.csproj -c Release -o /app/publish

# ============================================
# Runtime
# ============================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copiar archivos publicados
COPY --from=publish /app/publish .

# Exponer puerto
EXPOSE 8080

# Variables de entorno
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Comando de inicio
ENTRYPOINT ["dotnet", "DondeSalimos.Server.dll"]
