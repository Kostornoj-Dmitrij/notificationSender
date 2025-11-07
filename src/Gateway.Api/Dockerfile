FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_ENVIRONMENT=Docker
ENV ASPNETCORE_URLS=http://*:8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Common.DTO/Common.DTO.csproj", "src/Common.DTO/"]
COPY ["src/Common.Messaging/Common.Messaging.csproj", "src/Common.Messaging/"]
COPY ["src/Common.Utils/Common.Utils.csproj", "src/Common.Utils/"]
COPY ["src/Gateway.Api/Gateway.Api.csproj", "src/Gateway.Api/"]
RUN dotnet restore "src/Gateway.Api/Gateway.Api.csproj"
COPY . .
WORKDIR "/src/src/Gateway.Api"
RUN dotnet build "Gateway.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Gateway.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Gateway.Api.dll"]