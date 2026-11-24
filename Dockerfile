# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY tasks/tasks.csproj tasks/
RUN dotnet restore tasks/tasks.csproj

COPY tasks/. tasks/
RUN dotnet publish tasks/tasks.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["sh", "-c", "dotnet StudyFlow.dll --urls http://0.0.0.0:${PORT:-8080}"]
