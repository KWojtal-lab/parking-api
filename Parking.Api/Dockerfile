FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY Parking.Api.csproj ./
RUN dotnet restore Parking.Api.csproj

COPY . .
RUN dotnet publish Parking.Api.csproj -c Release -o /app --no-restore

# Finishing the image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

EXPOSE 8080
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "Parking.Api.dll"]