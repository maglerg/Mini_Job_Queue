# Этап 1. Сборка приложения
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Копируем файл проекта
COPY Mini_Job_Queue.API.csproj ./

# Восстанавливаем зависимости
RUN dotnet restore

# Копируем остальные файлы
COPY . .

# Компилируем приложение
RUN dotnet publish -c Release -o /app/publish --no-restore


# Этап 2. Запуск приложения
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

# Порт, который слушает приложение внутри контейнера
EXPOSE 8080

ENV ASPNETCORE_HTTP_PORTS=8080

ENTRYPOINT ["dotnet", "Mini_Job_Queue.API.dll"]