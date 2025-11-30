FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Push.Tester/Push.Tester.csproj", "src/Push.Tester/"]
RUN dotnet restore "src/Push.Tester/Push.Tester.csproj"
COPY . .
WORKDIR "/src/src/Push.Tester"
RUN dotnet build "Push.Tester.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Push.Tester.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY src/Push.Tester/wwwroot ./wwwroot
ENTRYPOINT ["dotnet", "Push.Tester.dll"]