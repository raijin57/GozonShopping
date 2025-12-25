FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Gozon.ApiGateway/Gozon.ApiGateway.csproj", "src/Gozon.ApiGateway/"]
RUN dotnet restore "src/Gozon.ApiGateway/Gozon.ApiGateway.csproj"
COPY . .
WORKDIR "/src/src/Gozon.ApiGateway"
RUN dotnet publish "Gozon.ApiGateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Gozon.ApiGateway.dll"]

