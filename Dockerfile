# Етап 1: Збірка (Build) - Використовуємо SDK 9.0
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Копіюємо файл проекту і відновлюємо залежності
COPY ["ProjectsDonetskWaterHope.csproj", "./"]
RUN dotnet restore "ProjectsDonetskWaterHope.csproj"

# Копіюємо решту файлів і збираємо проект
COPY . .
WORKDIR "/src/."
RUN dotnet build "ProjectsDonetskWaterHope.csproj" -c Release -o /app/build

# Публікуємо (Publish)
FROM build AS publish
RUN dotnet publish "ProjectsDonetskWaterHope.csproj" -c Release -o /app/publish

# Етап 2: Запуск (Runtime) - Використовуємо ASP.NET 9.0
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProjectsDonetskWaterHope.dll"]