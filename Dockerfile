#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["ConsoleEncoder.csproj", "ConsoleEncoder/"]
COPY ["Impinj.Atlas.dll", "ConsoleEncoder/"]
COPY ["ImpinjLogger.dll", "ConsoleEncoder/"]
COPY ["TagDataTranslation.dll", "ConsoleEncoder/"]
COPY ["localhost.pfx", "ConsoleEncoder/"]
RUN dotnet restore "ConsoleEncoder/ConsoleEncoder.csproj"
COPY . .
WORKDIR "/src/ConsoleEncoder"
RUN dotnet build "ConsoleEncoder.csproj" -c Release -o /app/build

FROM build AS publish
#RUN dotnet publish "ConsoleEncoder.csproj" -c Release -o /app/publish --self-contained false --no-restore
#RUN dotnet publish "ConsoleEncoder.csproj" -c Release -o /app/publish -r linux-arm --self-contained false --no-restore
RUN dotnet publish "ConsoleEncoder.csproj" /p:PublishProfile=ConsoleEncoder/Properties/PublishProfiles/FolderProfile.pubxml

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ConsoleEncoder.dll"]