# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug config)
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
# Install ICU libs for localization support, LibreOffice for conversion, Java for some doc formats, and fonts (including Chinese)
RUN apk add --no-cache icu-libs libreoffice ttf-dejavu ttf-liberation \
    font-noto-cjk font-noto-emoji terminus-font
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["OpenPrinterWeb/OpenPrinterWeb.csproj", "OpenPrinterWeb/"]
RUN dotnet restore "./OpenPrinterWeb/OpenPrinterWeb.csproj"
COPY . .
WORKDIR "/src/OpenPrinterWeb"
RUN dotnet build "./OpenPrinterWeb.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./OpenPrinterWeb.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create writable directory and set permissions for the non-root user
USER root
RUN mkdir -p /app/wwwroot/uploads && chown -R $APP_UID:$APP_UID /app/wwwroot
USER $APP_UID

# Support graceful exit: Increase shutdown timeout for SignalR and background tasks
ENV DOTNET_SHUTDOWN_TIMEOUT_SECONDS=30

ENTRYPOINT ["dotnet", "OpenPrinterWeb.dll"]
