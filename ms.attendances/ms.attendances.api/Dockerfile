#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["ms.attendances/ms.attendances.api/ms.attendances.api.csproj", "ms.attendances/ms.attendances.api/"]
COPY ["ms.attendances/ms.attendances.application/ms.attendances.application.csproj", "ms.attendances/ms.attendances.application/"]
COPY ["ms.attendances/ms.attendances.domain/ms.attendances.domain.csproj", "ms.attendances/ms.attendances.domain/"]
COPY ["ms.attendances/ms.attendances.infraestucture/ms.attendances.infraestucture.csproj", "ms.attendances/ms.attendances.infraestucture/"]
RUN dotnet restore "ms.attendances/ms.attendances.api/ms.attendances.api.csproj"
COPY . .
WORKDIR "/src/ms.attendances/ms.attendances.api"
RUN dotnet build "ms.attendances.api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ms.attendances.api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ms.attendances.api.dll"]