# Usar la imagen base de .NET 8 SDK para la compilación
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Establecer el directorio de trabajo en la imagen
WORKDIR /src

# Copiar el archivo de proyecto y restaurar las dependencias
COPY ["CrudProductos/CrudProductos.csproj", "CrudProductos/"]
RUN dotnet restore "CrudProductos/CrudProductos.csproj"

# Copiar el resto del código y construir el proyecto
COPY . .
RUN dotnet build "CrudProductos.csproj" -c Release -o /app/build

# Publicar el proyecto
RUN dotnet publish "CrudProductos.csproj" -c Release -o /app/publish

# Establecer la imagen base de runtime para el contenedor final
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

# Copiar el proyecto publicado
COPY --from=build /app/publish .

# Comando para ejecutar la aplicación
ENTRYPOINT ["dotnet", "CrudProductos.dll"]
