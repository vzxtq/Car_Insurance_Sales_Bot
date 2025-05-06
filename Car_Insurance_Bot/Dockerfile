FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY *.csproj ./
COPY appsettings.json .

RUN dotnet restore

COPY . .
RUN dotnet build ./Car_Insurance_Bot.csproj -c Release -o /app/build

FROM build AS publish
COPY appsettings.json /app/
RUN dotnet publish ./Car_Insurance_Bot.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 80

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Car_Insurance_Bot.dll"]
