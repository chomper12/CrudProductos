# Usa la imagen base de .NET
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

# Usa la imagen SDK de .NET para compilar
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar el archivo .csproj y restaurar dependencias
COPY ["CrudProductos.csproj", "CrudProductos/"]
RUN dotnet restore "CrudProductos/CrudProductos.csproj"

# Copiar todo el contenido del proyecto
COPY . .

# Configurar el directorio de trabajo y compilar el proyecto
WORKDIR "/src/CrudProductos"
RUN dotnet build "CrudProductos.csproj" -c Release -o /app/build

# Publicar la aplicación
FROM build AS publish
RUN dotnet publish "CrudProductos.csproj" -c Release -o /app/publish

# Crear una nueva capa para el contenedor final
FROM base AS final
WORKDIR /app

# Copiar los archivos publicados
COPY --from=publish /app/publish .

# Copiar las DLLs desde wwwroot/lib a la imagen
COPY ./wwwroot/lib /app/wwwroot/lib

# Asegurarse de que las DLLs estén referenciadas correctamente en el proyecto
RUN echo "Verificando las DLLs en el contenedor" && ls /app/wwwroot/lib

# Establecer el punto de entrada para la aplicación
ENTRYPOINT ["dotnet", "CrudProductos.dll"]
