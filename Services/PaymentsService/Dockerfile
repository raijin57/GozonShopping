FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Services/PaymentsService/PaymentsService.csproj", "Services/PaymentsService/"]
COPY ["Shared/Contracts/Shared.Contracts.csproj", "Shared/Contracts/"]
RUN dotnet restore "Services/PaymentsService/PaymentsService.csproj"
COPY . .
WORKDIR "/src/Services/PaymentsService"
RUN dotnet publish "PaymentsService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PaymentsService.dll"]

