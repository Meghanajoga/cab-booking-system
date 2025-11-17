FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS runtime

# === ADD SSL FIXES HERE ===
WORKDIR /app

# Install OpenSSL and update CA certificates
RUN apt-get update && \
    apt-get install -y \
    openssl \
    ca-certificates && \
    update-ca-certificates && \
    rm -rf /var/lib/apt/lists/* && \
    openssl version

# ==========================

COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://*:8080

# Set environment variables for SSL
ENV DOTNET_SYSTEM_NET_HTTP_USESOCKETSHTTPHANDLER=0
ENV MONGO_DRIVER_COMPRESSOR=zlib

ENTRYPOINT ["dotnet", "CabBookingSystem.dll"]