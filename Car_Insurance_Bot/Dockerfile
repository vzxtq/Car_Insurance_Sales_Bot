FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

COPY Car_Insurance_Bot/Car_Insurance_Bot.csproj Car_Insurance_Bot/
RUN dotnet restore "Car_Insurance_Bot/Car_Insurance_Bot.csproj"

COPY . .
WORKDIR /src/Car_Insurance_Bot
RUN dotnet publish -c Release -o /app

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Car_Insurance_Bot.dll"]
