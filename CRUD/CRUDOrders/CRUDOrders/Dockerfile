FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["CRUDOrders/CRUDOrders.csproj", "CRUDOrders/"]
RUN dotnet restore "CRUDOrders/CRUDOrders.csproj"

COPY . .
WORKDIR "/src/CRUDOrders"
RUN dotnet build "CRUDOrders.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CRUDOrders.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 5000 5001
ENV ASPNETCORE_URLS=http://+:5000;https://+:5001
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "CRUDOrders.dll"]
