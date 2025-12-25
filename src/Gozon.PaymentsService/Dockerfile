FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Gozon.PaymentsService/Gozon.PaymentsService.csproj", "src/Gozon.PaymentsService/"]
COPY ["src/Gozon.Shared/Contracts/Gozon.Shared.csproj", "src/Gozon.Shared/Contracts/"]
RUN dotnet restore "src/Gozon.PaymentsService/Gozon.PaymentsService.csproj"
COPY . .
WORKDIR "/src/src/Gozon.PaymentsService"
RUN dotnet publish "Gozon.PaymentsService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Gozon.PaymentsService.dll"]

