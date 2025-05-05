FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Car_Insurance_Bot.sln ./
COPY Car_Insurance_Bot.csproj ./
RUN dotnet restore "Car_Insurance_Bot.csproj"

COPY . .
RUN dotnet build "Car_Insurance_Bot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Car_Insurance_Bot.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

COPY --from=publish /app/publish ./
COPY appsettings.json /app/
ENTRYPOINT ["dotnet", "Car_Insurance_Bot.dll"]
