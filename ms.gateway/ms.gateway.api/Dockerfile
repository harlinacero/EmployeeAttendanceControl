#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Directory.Packages.props", "./"]
COPY ["ms.gateway/ms.gateway.api/ms.gateway.api.csproj", "ms.gateway/ms.gateway.api/"]
RUN dotnet restore "ms.gateway/ms.gateway.api/ms.gateway.api.csproj"
COPY . .
WORKDIR "/src/ms.gateway/ms.gateway.api"
RUN dotnet build "ms.gateway.api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ms.gateway.api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ms.gateway.api.dll"]
