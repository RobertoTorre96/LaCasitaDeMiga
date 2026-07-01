# Etapa de compilación
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Copiar todo y restaurar/compilar
COPY . ./
RUN dotnet restore
RUN dotnet publish -c Release -o out

# Etapa de runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app/out .

# Configurar el puerto para Render
ENV ASPNETCORE_URLS=http://+:10000

ENTRYPOINT ["dotnet", "LaCasitaDeMiga.dll"]