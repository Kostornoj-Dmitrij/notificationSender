FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/Common.DTO/Common.DTO.csproj", "src/Common.DTO/"]
COPY ["src/Common.Messaging/Common.Messaging.csproj", "src/Common.Messaging/"]
COPY ["src/Common.Utils/Common.Utils.csproj", "src/Common.Utils/"]
COPY ["src/Sms.Service/Sms.Service.csproj", "src/Sms.Service/"]

RUN dotnet restore "src/Sms.Service/Sms.Service.csproj"

COPY . .

WORKDIR "/src/src/Sms.Service"
RUN dotnet build "Sms.Service.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Sms.Service.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Sms.Service.dll"]