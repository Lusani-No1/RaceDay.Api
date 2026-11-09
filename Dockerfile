FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY RaceDay.Api.csproj ./
RUN dotnet restore RaceDay.Api.csproj

COPY . ./
RUN dotnet publish RaceDay.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Local uploads are written here; mount a volume to keep them between containers.
RUN mkdir -p /app/wwwroot/uploads

COPY --from=build /app/publish ./

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "RaceDay.Api.dll"]
