FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_ENVIRONMENT=Docker
ENV ASPNETCORE_URLS=http://*:8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/Common.DTO/Common.DTO.csproj", "src/Common.DTO/"]
COPY ["src/Common.Utils/Common.Utils.csproj", "src/Common.Utils/"]
COPY ["src/Status.Api/Status.Api.csproj", "src/Status.Api/"]

RUN dotnet restore "src/Status.Api/Status.Api.csproj"
COPY . .
WORKDIR "/src/src/Status.Api"
RUN dotnet build "Status.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Status.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY ["src/Status.Api/appsettings.json", "./"]
COPY ["src/Status.Api/appsettings.Docker.json", "./"]
ENTRYPOINT ["dotnet", "Status.Api.dll"]