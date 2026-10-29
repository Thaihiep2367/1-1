# Multi-stage Dockerfile cho .NET 10 Clean Architecture
# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files & restore dependencies
COPY ["src/DoanPhi.Domain/DoanPhi.Domain.csproj", "src/DoanPhi.Domain/"]
COPY ["src/DoanPhi.Application/DoanPhi.Application.csproj", "src/DoanPhi.Application/"]
COPY ["src/DoanPhi.Infrastructure/DoanPhi.Infrastructure.csproj", "src/DoanPhi.Infrastructure/"]
COPY ["src/DoanPhi.API/DoanPhi.API.csproj", "src/DoanPhi.API/"]

RUN dotnet restore "src/DoanPhi.API/DoanPhi.API.csproj"

# Copy full source code & publish
COPY . .
WORKDIR "/src/src/DoanPhi.API"
RUN dotnet publish "DoanPhi.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 5000
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "DoanPhi.API.dll"]
