FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY Parking.Auth.csproj ./
RUN dotnet restore Parking.Auth.csproj

COPY . .
RUN dotnet restore Parking.Auth.csproj
RUN dotnet publish Parking.Auth.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
EXPOSE 8081
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "Parking.Auth.dll"]
