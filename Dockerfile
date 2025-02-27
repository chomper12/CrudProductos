# Usa una imagen base de .NET SDK
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copia los archivos del proyecto y restaura las dependencias
COPY *.csproj ./
RUN dotnet restore

# Copia todo el resto del código y publica la aplicación
COPY . ./
RUN dotnet publish -c Release -o out

# Usa una imagen base de .NET Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/out .

# Define el comando de inicio
ENTRYPOINT ["dotnet", "CrudProductos.dll"]
