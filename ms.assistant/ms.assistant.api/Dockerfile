#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Directory.Packages.props", "./"]
COPY ["ms.assistant/ms.assistant.api/ms.assistant.api.csproj", "ms.assistant/ms.assistant.api/"]
COPY ["ms.assistant/ms.assistant.application/ms.assistant.application.csproj", "ms.assistant/ms.assistant.application/"]
COPY ["ms.assistant/ms.assistant.domain/ms.assistant.domain.csproj", "ms.assistant/ms.assistant.domain/"]
COPY ["ms.assistant/ms.assistant.infraestucture/ms.assistant.infraestucture.csproj", "ms.assistant/ms.assistant.infraestucture/"]
RUN dotnet restore "ms.assistant/ms.assistant.api/ms.assistant.api.csproj"
COPY . .
WORKDIR "/src/ms.assistant/ms.assistant.api"
RUN dotnet build "ms.assistant.api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ms.assistant.api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ms.assistant.api.dll"]
