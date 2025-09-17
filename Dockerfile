FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["irai.csproj", "./"]
RUN dotnet restore "irai.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "irai.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "irai.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS run
USER $APP_UID
WORKDIR /app
EXPOSE 5076
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "irai.dll"]
