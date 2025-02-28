# Usa la imagen oficial de .NET 8 para producción
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

# Imagen de .NET SDK para construir la aplicación
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["CrudProductos.csproj", "./"]
RUN dotnet restore "./CrudProductos.csproj"
COPY . .
RUN dotnet publish "./CrudProductos.csproj" -c Release -o /app/publish

# Imagen final para ejecutar la aplicación
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
RUN chmod -R 777 /app  # 👈 Ajusta permisos para evitar errores
ENTRYPOINT ["dotnet", "CrudProductos.dll"]
