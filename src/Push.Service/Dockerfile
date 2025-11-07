FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/Common.DTO/Common.DTO.csproj", "src/Common.DTO/"]
COPY ["src/Common.Messaging/Common.Messaging.csproj", "src/Common.Messaging/"]
COPY ["src/Common.Utils/Common.Utils.csproj", "src/Common.Utils/"]
COPY ["src/Push.Service/Push.Service.csproj", "src/Push.Service/"]

RUN dotnet restore "src/Push.Service/Push.Service.csproj"

COPY . .

WORKDIR "/src/src/Push.Service"
RUN dotnet build "Push.Service.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Push.Service.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Push.Service.dll"]