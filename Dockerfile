# Етап 1: Збірка проєкту
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["LibraryMVC/LibraryMVC.csproj", "LibraryMVC/"]
RUN dotnet restore "LibraryMVC/LibraryMVC.csproj"
COPY . .
WORKDIR "/src/LibraryMVC"
RUN dotnet build "LibraryMVC.csproj" -c Release -o /app/build

# Етап 2: Публікація
FROM build AS publish
RUN dotnet publish "LibraryMVC.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Етап 3: Створення фінального образу
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

# Встановлюємо українську локаль
RUN apt-get update && apt-get install -y locales && locale-gen uk_UA.UTF-8

WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LibraryMVC.dll"]