FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY backend/FixMyCampus.Api/FixMyCampus.Api.csproj backend/FixMyCampus.Api/
COPY backend/FixMyCampus.Application/FixMyCampus.Application.csproj backend/FixMyCampus.Application/
COPY backend/FixMyCampus.Domain/FixMyCampus.Domain.csproj backend/FixMyCampus.Domain/
COPY backend/FixMyCampus.Infrastructure/FixMyCampus.Infrastructure.csproj backend/FixMyCampus.Infrastructure/

RUN dotnet restore backend/FixMyCampus.Api/FixMyCampus.Api.csproj

COPY backend/ backend/

RUN dotnet publish backend/FixMyCampus.Api/FixMyCampus.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "FixMyCampus.Api.dll"]
