#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["ms.users/ms.users.api/ms.users.api.csproj", "ms.users/ms.users.api/"]
COPY ["ms.users/ms.users.application/ms.users.application.csproj", "ms.users/ms.users.application/"]
COPY ["ms.users/ms.users.domain/ms.users.domain.csproj", "ms.users/ms.users.domain/"]
COPY ["ms.users/ms.users.infraestructure/ms.users.infraestructure.csproj", "ms.users/ms.users.infraestructure/"]
RUN dotnet restore "ms.users/ms.users.api/ms.users.api.csproj"
COPY . .
WORKDIR "/src/ms.users/ms.users.api"
RUN dotnet build "ms.users.api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ms.users.api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ms.users.api.dll"]