FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
ENV DOTNET_NUGET_SIGNATURE_VERIFICATION=false
WORKDIR /src
COPY ["ContainerRfBot.API/ContainerRfBot.API.csproj", "ContainerRfBot.API/"]
COPY ["ContainerRfBot.Core/ContainerRfBot.Core.csproj", "ContainerRfBot.Core/"]
COPY ["ContainerRfBot.Infrastructure/ContainerRfBot.Infrastructure.csproj", "ContainerRfBot.Infrastructure/"]
COPY ["ContainerRfBot.Bot/ContainerRfBot.Bot.csproj", "ContainerRfBot.Bot/"]
RUN dotnet restore "ContainerRfBot.API/ContainerRfBot.API.csproj"
COPY . .
WORKDIR "/src/ContainerRfBot.API"
RUN dotnet build "ContainerRfBot.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "ContainerRfBot.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

# ==========================================
# НАЧАЛО: Установка доверенных сертификатов
# ==========================================
USER root

# Копируем все 5 сертификатов в системную папку доверенных сертификатов
COPY ContainerRfBot.API/Certs/russian_trusted_root_ca_gost_2025_pem.crt /usr/local/share/ca-certificates/russian_trusted_root_ca_gost_2025_pem.crt
COPY ContainerRfBot.API/Certs/russian_trusted_root_ca_pem.crt /usr/local/share/ca-certificates/russian_trusted_root_ca_pem.crt
COPY ContainerRfBot.API/Certs/russian_trusted_sub_ca_2024_pem.crt /usr/local/share/ca-certificates/russian_trusted_sub_ca_2024_pem.crt
COPY ContainerRfBot.API/Certs/russian_trusted_sub_ca_gost_2025_pem.crt /usr/local/share/ca-certificates/russian_trusted_sub_ca_gost_2025_pem.crt
COPY ContainerRfBot.API/Certs/russian_trusted_sub_ca_pem.crt /usr/local/share/ca-certificates/russian_trusted_sub_ca_pem.crt

# Обновляем хранилище сертификатов (создаст хеши в /etc/ssl/certs/)
RUN update-ca-certificates

# Возвращаем безопасного пользователя для запуска приложения
USER $APP_UID
# ==========================================
# КОНЕЦ: Установка доверенных сертификатов
# ==========================================

COPY --from=publish /app/publish .
ENV DOTNET_SYSTEM_NET_DISABLEIPV6=1

ENTRYPOINT ["dotnet", "ContainerRfBot.API.dll"]