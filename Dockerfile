# Etapa base: runtime de ASP.NET 9
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

# Render suele usar el puerto 10000 dentro del contenedor
EXPOSE 10000

# Etapa de build: SDK de .NET 9
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiamos el csproj y restauramos dependencias
COPY ["DFindApi.csproj", "./"]
RUN dotnet restore "DFindApi.csproj"

# Copiamos el resto del código y publicamos
COPY . .
RUN dotnet publish "DFindApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Etapa final
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# ASP.NET escuchando en el puerto 10000 dentro del contenedor
ENV ASPNETCORE_URLS=http://0.0.0.0:10000

ENTRYPOINT ["dotnet", "DFindApi.dll"]
