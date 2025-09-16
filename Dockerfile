# Stage 1: build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# копируем решение и проекты для restore
COPY *.sln .
COPY GuessItAPI/*.csproj ./GuessItAPI/
RUN dotnet restore

# копируем все файлы и билдим
COPY . .
WORKDIR /app/GuessItAPI
RUN dotnet publish -c Release -o /app/publish

# Stage 2: runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 80
ENTRYPOINT ["dotnet", "GuessItAPI.dll"]
