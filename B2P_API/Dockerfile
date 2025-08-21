# =========================
# BUILD STAGE
# =========================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Chỉ copy file csproj của API (không kéo test vào)
COPY B2P_API/*.csproj ./B2P_API/
RUN dotnet restore B2P_API/B2P_API.csproj

# Copy toàn bộ code, nhưng Docker chỉ build project API
COPY . .
WORKDIR /src/B2P_API

# Chỉ publish project API
RUN dotnet publish B2P_API.csproj -c Release -o /app

# =========================
# RUNTIME STAGE
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .
EXPOSE 80
ENTRYPOINT ["dotnet", "B2P_API.dll"]
