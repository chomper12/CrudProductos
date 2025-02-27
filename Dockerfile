# Usa la imagen base de .NET
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["CrudProductos/CrudProductos.csproj", "CrudProductos/"]
RUN dotnet restore "CrudProductos/CrudProductos.csproj"
COPY . .
WORKDIR "/src/CrudProductos"
RUN dotnet build "CrudProductos.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CrudProductos.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CrudProductos.dll"]
