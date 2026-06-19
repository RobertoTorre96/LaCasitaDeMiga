# Etapa de compilación
using static System.Net.WebRequestMethods;

FROM mcr.microsoft.com / dotnet / sdk:8.0 AS build-env
WORKDIR /app

# Copiar todo y restaurar/compilar
COPY . ./
RUN dotnet restore
RUN dotnet publish -c Release -o out

# Etapa de runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR / app
COPY--from = build - env / app /out .

# Configurar el puerto para Render (Render usa la variable de entorno PORT)
ENV ASPNETCORE_URLS = http://+:10000

ENTRYPOINT["dotnet", "ECommersAPI.dll"]
# ⚠️ NOTA: Reemplazá "WebApplication1.dll" por el nombre real de tu archivo de salida si tu proyecto se llama distinto.